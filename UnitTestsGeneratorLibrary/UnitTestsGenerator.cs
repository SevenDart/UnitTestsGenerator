﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnitTestsGeneratorLibrary.Models;

namespace UnitTestsGeneratorLibrary
{
    public class UnitTestsGenerator : IUnitTestsGenerator
    {
        private readonly TransformBlock<string, TestClassEnvironment> _readFileBlock;
        private readonly TransformBlock<TestClassEnvironment, TestClassEnvironment> _parseFileBlock;
        private readonly TransformBlock<TestClassEnvironment, TestClassEnvironment> _createTestSyntaxTreeBlock;
        private readonly ActionBlock<TestClassEnvironment> _writeToFileBlock;

        private GeneratorConfig _generatorConfig;

        public UnitTestsGenerator()
        {
            _readFileBlock = new TransformBlock<string, TestClassEnvironment>(ReadFile);
            _parseFileBlock = new TransformBlock<TestClassEnvironment, TestClassEnvironment>(ParseFile);
            _createTestSyntaxTreeBlock = new TransformBlock<TestClassEnvironment, TestClassEnvironment>(GenerateSyntaxTreeOfTestClass);
            _writeToFileBlock = new ActionBlock<TestClassEnvironment>(WriteToFile);

            var linkOptions = new DataflowLinkOptions() {PropagateCompletion = true};
            
            _readFileBlock.LinkTo(_parseFileBlock, linkOptions);
            _parseFileBlock.LinkTo(_createTestSyntaxTreeBlock, linkOptions);
            _createTestSyntaxTreeBlock.LinkTo(_writeToFileBlock, linkOptions);
        }
        
        
        public async Task GenerateTests(GeneratorConfig generatorConfig)
        {
            _generatorConfig = generatorConfig;
            foreach (var filename in _generatorConfig.Filenames)
            {
                _readFileBlock.Post(filename);
                await _writeToFileBlock.Completion;
            }
            
        }

        //First TransformBlock<string, TestEnvironment> to read file
        private async Task<TestClassEnvironment> ReadFile(string filename)
        {
            using var streamReader = new StreamReader(filename);

            var testFilename = Path.GetFileName(filename);
            testFilename = testFilename[..^2] + "Tests.cs";
            
            return new TestClassEnvironment()
            {
                Filename = $"{_generatorConfig.EndpointFolder}\\{testFilename}",
                SourceText = await streamReader.ReadToEndAsync()
            };
        }

        //Second TransformBlock<TestEnvironment,TestEnvironment> to parse file
        private async Task<TestClassEnvironment> ParseFile(TestClassEnvironment testClassEnvironment)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(testClassEnvironment.SourceText);

            var rootNode = await syntaxTree.GetRootAsync();

            testClassEnvironment.TestingClassModels = TestingClassModel.ParseSyntaxNode(rootNode);

            testClassEnvironment.TestingClassModels = 
                testClassEnvironment
                    .TestingClassModels
                    .Where(classModel => classModel.Methods.Count > 0)
                    .ToList();
            
            return testClassEnvironment.TestingClassModels.Count > 0 
                ? testClassEnvironment 
                : null;
        }
        
        //Third ActionBlock<TestEnvironment> to create a syntaxTree
        private TestClassEnvironment GenerateSyntaxTreeOfTestClass(TestClassEnvironment testClassEnvironment)
        {
            var root = SyntaxFactory.CompilationUnit();

            //Add namespaces to file
            foreach (var namespaceName in testClassEnvironment.TestingClassModels[0].UsedNamespaces)
            {
                var usedNamespace = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName));
                root = root.AddUsings(usedNamespace);
            }
            root = root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("NUnit.Framework")));
            root = root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Moq")));

            //Create namespace for file
            var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(new DirectoryInfo(_generatorConfig.EndpointFolder).Name));

            //Create test classes
            foreach (var classModel in testClassEnvironment.TestingClassModels)
            {
                var testClassDeclaration = SyntaxFactory.ClassDeclaration(classModel.ClassName + "Tests");

                //Create mocked dependencies for class
                foreach (var constructorArgument in classModel.ConstructorArguments)
                {
                    FieldDeclarationSyntax mockedArgument;
                    if (constructorArgument.ArgType[0] == 'I')
                    {
                        mockedArgument = SyntaxFactory.FieldDeclaration(
                            SyntaxFactory.VariableDeclaration(
                                SyntaxFactory.ParseTypeName("Mock<" + constructorArgument.ArgType + ">")
                            ).AddVariables(SyntaxFactory.VariableDeclarator("_" + constructorArgument.Name))
                        );
                    }
                    else
                    {
                        mockedArgument = SyntaxFactory.FieldDeclaration(
                            SyntaxFactory.VariableDeclaration(
                                SyntaxFactory.ParseTypeName(constructorArgument.ArgType)
                            ).AddVariables(SyntaxFactory.VariableDeclarator("_" + constructorArgument.Name))
                        );
                    }

                    testClassDeclaration = testClassDeclaration.AddMembers(mockedArgument);
                }
                
                //Create testing class itself
                var testingClassDeclaration = SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.ParseTypeName(classModel.ClassName)
                    ).AddVariables(SyntaxFactory.VariableDeclarator($"_{Char.ToLower(classModel.ClassName[0])}{classModel.ClassName[1..]}"))
                );
                testClassDeclaration = testClassDeclaration.AddMembers(testingClassDeclaration);
                
                //Create setup method
                var setupMethod = SyntaxFactory
                    .MethodDeclaration(SyntaxFactory.ParseTypeName("void"), "Setup")
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddAttributeLists(
                    SyntaxFactory.AttributeList(
                        SyntaxFactory.SeparatedList<AttributeSyntax>().Add(
                            SyntaxFactory.Attribute(SyntaxFactory.ParseName("SetUp"))
                            )
                        )
                    );

                //construct dependencies
                var constructorArgs = new StringBuilder();
                foreach (var dependency in classModel.ConstructorArguments)
                {
                    string statementTexted;
                    if (dependency.ArgType[0] == 'I')
                    {
                        constructorArgs.Append($"_{dependency.Name}.Object,");
                        statementTexted = $"_{dependency.Name} = new Mock<{dependency.ArgType}>();";                        
                    }
                    else
                    {
                        constructorArgs.Append($"_{dependency.Name},");
                        statementTexted = $"_{dependency.Name} = {ArgumentModel.GetDefaultValue(dependency.TryToParseType())};";
                    }
                    var constructStatement = SyntaxFactory.ParseStatement(statementTexted);
                    setupMethod = setupMethod.AddBodyStatements(constructStatement);
                }

                if (constructorArgs.Length != 0)
                {
                    constructorArgs.Remove(constructorArgs.Length - 1, 1);
                }
                
                //construct testing class
                var classVariable = $"_{Char.ToLower(classModel.ClassName[0])}{classModel.ClassName[1..]}";
                var classConstructTexted = $"{classVariable} = new {classModel.ClassName}({constructorArgs});";
                var constructClassStatement = SyntaxFactory.ParseStatement(classConstructTexted);
                setupMethod = setupMethod.AddBodyStatements(constructClassStatement);
                
                testClassDeclaration = testClassDeclaration.AddMembers(setupMethod);
                
                //create tests
                foreach (var testingMethod in classModel.Methods)
                {
                    //create test method
                    var methodDeclaration = SyntaxFactory
                        .MethodDeclaration(SyntaxFactory.ParseTypeName("void"), $"{testingMethod.MethodName}_Test")
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddAttributeLists(
                        SyntaxFactory.AttributeList(
                            SyntaxFactory.SeparatedList<AttributeSyntax>().Add(
                                SyntaxFactory.Attribute(SyntaxFactory.ParseName("Test"))
                            )
                        )
                    );

                    //Arrange statements
                    var methodArgs = new StringBuilder();
                    foreach (var argument in testingMethod.Arguments)
                    {
                        methodArgs.Append($"{argument.Name},");
                        var defaultValue = ArgumentModel.GetDefaultValue(argument.TryToParseType());
                        var stringifyDefaultValue = defaultValue == null ? "null" : defaultValue.ToString();
                        var textedStatement = $"{argument.ArgType} {argument.Name} = {stringifyDefaultValue};";
                        var parsedStatement = SyntaxFactory.ParseStatement(textedStatement);
                        methodDeclaration = methodDeclaration.AddBodyStatements(parsedStatement);
                    }
                    
                    if (methodArgs.Length != 0)
                    {
                        methodArgs.Remove(methodArgs.Length - 1, 1);
                    }
                    
                    //Act statement
                    var actStatement = $"{classVariable}.{testingMethod.MethodName}({methodArgs});";
                    if (testingMethod.ReturnType.TryToParseType() != typeof(void))
                    {
                        actStatement = $"{testingMethod.ReturnType.ArgType} result = " + actStatement;
                    }
                    var parsedActStatement = SyntaxFactory.ParseStatement(actStatement);
                    methodDeclaration = methodDeclaration.AddBodyStatements(parsedActStatement);
                    
                    
                    //Assert statements
                    if (testingMethod.ReturnType.TryToParseType() != typeof(void))
                    {
                        var defaultValue = ArgumentModel.GetDefaultValue(testingMethod.ReturnType.TryToParseType());
                        var stringifyDefaultValue = defaultValue == null ? "null" : defaultValue.ToString();
                        var expectedResult = SyntaxFactory.ParseStatement(
                            $"{testingMethod.ReturnType.ArgType} expected = {stringifyDefaultValue};"
                            );
                        var expectedAssert = SyntaxFactory.ParseStatement("Assert.That(result, Is.EqualTo(expected));");

                        methodDeclaration = methodDeclaration.AddBodyStatements(expectedResult, expectedAssert);
                    }
                    
                    var failAssert = SyntaxFactory.ParseStatement("Assert.Fail(\"autogenerated\");");
                    methodDeclaration = methodDeclaration.AddBodyStatements(failAssert);
                    
                    testClassDeclaration = testClassDeclaration.AddMembers(methodDeclaration);
                }

                namespaceDeclaration = namespaceDeclaration.AddMembers(testClassDeclaration);
            }

            root = root.AddMembers(namespaceDeclaration);

            testClassEnvironment.GeneratedSyntaxTree = SyntaxFactory.SyntaxTree(root);

            return testClassEnvironment;
        }

        //Fourth ActionBlock<TestClassEnvironment> to write a file
        private async Task WriteToFile(TestClassEnvironment testClassEnvironment)
        {
            await using var streamWriter = new StreamWriter(testClassEnvironment.Filename);

            var root = await testClassEnvironment.GeneratedSyntaxTree.GetRootAsync();
            
            root.NormalizeWhitespace().WriteTo(streamWriter);
        }
    }
}
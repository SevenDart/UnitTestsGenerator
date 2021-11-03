using System;
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
            foreach (var filename in generatorConfig.Filenames)
            {
                _readFileBlock.Post(filename);
                await _writeToFileBlock.Completion;
            }
            
        }

        //First TransformBlock<string, TestEnvironment> to read file
        private async Task<TestClassEnvironment> ReadFile(string filename)
        {
            using var streamReader = new StreamReader(filename);

            return new TestClassEnvironment()
            {
                Filename = filename,
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
            var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("TestNamespace"));

            //Create test classes
            foreach (var classModel in testClassEnvironment.TestingClassModels)
            {
                var testClassDeclaration = SyntaxFactory.ClassDeclaration(classModel.ClassName + "Tests");

                //Create mocked dependencies for class
                foreach (var constructorArgument in classModel.ConstructorArguments)
                {
                    var mockedArgument = SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(
                            SyntaxFactory.ParseTypeName("Mock<" + constructorArgument.Name + ">")
                            ).AddVariables(SyntaxFactory.VariableDeclarator("_" + constructorArgument.Name))
                    );
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
                var setupMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), "Setup");

                setupMethod = setupMethod.AddAttributeLists(
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
                    constructorArgs.Append($"_{dependency.Name},");
                    var statementTexted = $"_{dependency.Name} = new Mock<{dependency.Type}>();";
                    var constructStatement = SyntaxFactory.ParseStatement(statementTexted);
                    setupMethod = setupMethod.AddBodyStatements(constructStatement);
                }

                if (constructorArgs.Length != 0)
                {
                    constructorArgs.Remove(constructorArgs.Length - 1, 1);
                }
                
                //construct testing class
                var classConstructTexted = $"_{Char.ToLower(classModel.ClassName[0])}{classModel.ClassName[1..]} = new {classModel.ClassName}({constructorArgs});";
                var constructClassStatement = SyntaxFactory.ParseStatement(classConstructTexted);
                setupMethod = setupMethod.AddBodyStatements(constructClassStatement);
                
                setupMethod = setupMethod.AddBodyStatements();

                testClassDeclaration = testClassDeclaration.AddMembers(setupMethod);
                
                namespaceDeclaration = namespaceDeclaration.AddMembers(testClassDeclaration);
            }

            root = root.AddMembers(namespaceDeclaration);

            testClassEnvironment.GeneratedSyntaxTree = SyntaxFactory.SyntaxTree(root);

            return testClassEnvironment;
        }


        //Fourth ActionBlock<TestClassEnvironment> to write a file
        private async Task WriteToFile(TestClassEnvironment testClassEnvironment)
        {
            await using var streamWriter = new StreamWriter(
                testClassEnvironment.Filename.Substring(0, testClassEnvironment.Filename.Length - 2)
                + "Tests.cs");

            var root = await testClassEnvironment.GeneratedSyntaxTree.GetRootAsync();
            
            root.NormalizeWhitespace().WriteTo(streamWriter);
        }
    }
}
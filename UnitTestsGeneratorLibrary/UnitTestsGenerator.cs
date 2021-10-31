using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using UnitTestsGeneratorLibrary.Models;

namespace UnitTestsGeneratorLibrary
{
    public class UnitTestsGenerator : IUnitTestsGenerator
    {
        public UnitTestsGenerator()
        {
            
        }
        
        
        public async Task GenerateTests(GeneratorConfig generatorConfig)
        {
            
        }

        //First TransformBlock<string, TestEnvironment> to read file
        private async Task<TestEnvironment> ReadFile(string filename)
        {
            using var streamReader = new StreamReader(filename);

            return new TestEnvironment()
            {
                Filename = filename,
                SourceText = await streamReader.ReadToEndAsync()
            };;
        }

        //Second TransformBlock<TestEnvironment,TestEnvironment> to parse file
        private async Task<TestEnvironment> ParseFile(TestEnvironment testEnvironment)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(testEnvironment.SourceText);

            var rootNode = await syntaxTree.GetRootAsync();

            testEnvironment.TestingClassModels = TestingClassModel.ParseSyntaxNode(rootNode);

            return testEnvironment;
        }
        
        //Third ActionBlock<TestEnvironment> to create test file
        private async Task GenerateTestEnvironment(TestEnvironment testEnvironment)
        {
            
        }
    }
}
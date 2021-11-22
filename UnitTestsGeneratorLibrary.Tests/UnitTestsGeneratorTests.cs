using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using UnitTestsGeneratorLibrary.Models;

namespace UnitTestsGeneratorLibrary.Tests
{
    public class Tests
    {
        private IUnitTestsGenerator _generator;

        private const string ClassAFilename = @"D:\CODE\Csharp\UnitTestsGenerator\SampleLibrary\ClassA.cs";
        private const string ClassBFilename = @"D:\CODE\Csharp\UnitTestsGenerator\SampleLibrary\ClassB.cs";

        private const string EndpointTestFolder = @"D:\CODE\Csharp\UnitTestsGenerator\SampleLibrary.Tests\";

        private async Task<TestClassEnvironment> ParseTestFile(string filename)
        {
            using var streamReader = new StreamReader(filename);
            var source = await streamReader.ReadToEndAsync();

            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var rootNode = await syntaxTree.GetRootAsync();

            var testClassEnvironment = new TestClassEnvironment
            {
                TestingClassModels = TestingClassModel.ParseSyntaxNode(rootNode)
            };

            return testClassEnvironment;
        }

        private void CleanTestFiles()
        {
            File.Delete(ClassAFilename);
            File.Delete(ClassBFilename);
        }
        
        [SetUp]
        public void Setup()
        {
            _generator = new UnitTestsGenerator();
        }

        [Test]
        public async Task CreateClassATests()
        {
            //Arrange
            var generatorConfig = new GeneratorConfig();
            
            generatorConfig.Filenames.Add(ClassAFilename);
            generatorConfig.EndpointFolder = EndpointTestFolder;
            
            //Act
            await _generator.GenerateTests(generatorConfig);
            var results = await ParseTestFile(ClassAFilename);

            //Assert
            var testMethods = results
                .TestingClassModels
                .SelectMany(tcm => tcm.Methods)
                .Where(m => m.MethodName.Contains("Test"))
                .ToList();
            
            Assert.AreEqual(3, testMethods.Count);
            CleanTestFiles();
        }
        
    }
}
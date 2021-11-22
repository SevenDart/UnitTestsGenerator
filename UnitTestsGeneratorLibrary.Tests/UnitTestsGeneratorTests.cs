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

        private const string TestClassAFilename = "ClassA.Tests.cs";
        private const string TestClassBFilename = "ClassB.Tests.cs";
        private const string ClassAFullPath = @"D:\CODE\Csharp\UnitTestsGenerator\SampleLibrary\ClassA.cs";
        private const string ClassBFullPath = @"D:\CODE\Csharp\UnitTestsGenerator\SampleLibrary\ClassB.cs";

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
            File.Delete(EndpointTestFolder + TestClassAFilename);
            File.Delete(EndpointTestFolder + TestClassBFilename);
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
            
            generatorConfig.Filenames.Add(ClassAFullPath);
            generatorConfig.EndpointFolder = EndpointTestFolder;
            
            //Act
            await _generator.GenerateTests(generatorConfig);
            var results = await ParseTestFile(EndpointTestFolder + TestClassAFilename);

            //Assert
            var testMethods = results
                .TestingClassModels
                .SelectMany(tcm => tcm.Methods)
                .Where(m => m.MethodName.Contains("Test"))
                .ToList();
            
            Assert.AreEqual(3, testMethods.Count);
            CleanTestFiles();
        }
        
        
        [Test]
        public async Task CreateClassBTests()
        {
            //Arrange
            var generatorConfig = new GeneratorConfig();
            
            generatorConfig.Filenames.Add(ClassBFullPath);
            generatorConfig.EndpointFolder = EndpointTestFolder;
            
            //Act
            await _generator.GenerateTests(generatorConfig);
            var results = await ParseTestFile(EndpointTestFolder + TestClassBFilename);

            //Assert
            var testMethods = results
                .TestingClassModels
                .SelectMany(tcm => tcm.Methods)
                .Where(m => m.MethodName.Contains("Test"))
                .ToList();
            
            Assert.AreEqual(1, testMethods.Count);
            CleanTestFiles();
        }
        
        [Test]
        public async Task CreateClassAandBTests()
        {
            //Arrange
            var generatorConfig = new GeneratorConfig();
            
            generatorConfig.Filenames.Add(ClassAFullPath);
            generatorConfig.Filenames.Add(ClassBFullPath);
            generatorConfig.EndpointFolder = EndpointTestFolder;
            
            //Act
            await _generator.GenerateTests(generatorConfig);
            var resultsOfClassA = await ParseTestFile(EndpointTestFolder + TestClassAFilename);
            var resultsOfClassB = await ParseTestFile(EndpointTestFolder + TestClassBFilename);

            //Assert
            var testMethodsOfA = resultsOfClassA
                .TestingClassModels
                .SelectMany(tcm => tcm.Methods)
                .Where(m => m.MethodName.Contains("Test"))
                .ToList();
            
            var testMethodsOfB = resultsOfClassB
                .TestingClassModels
                .SelectMany(tcm => tcm.Methods)
                .Where(m => m.MethodName.Contains("Test"))
                .ToList();
            
            Assert.AreEqual(3, testMethodsOfA.Count);
            Assert.AreEqual(1, testMethodsOfB.Count);
            CleanTestFiles();
        }
        
    }
}
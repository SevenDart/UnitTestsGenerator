using System;
using System.Threading.Tasks;
using UnitTestsGeneratorLibrary;

namespace SampleProject
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var generator = new UnitTestsGenerator();

            var generatorConfig = new GeneratorConfig();
            
            generatorConfig.Filenames.Add(@"D:\CODE\Csharp\UnitTestsGenerator\SampleLibrary\ClassA.cs");
            generatorConfig.Filenames.Add(@"D:\CODE\Csharp\UnitTestsGenerator\SampleLibrary\ClassB.cs");
            generatorConfig.Filenames.Add(@"D:\CODE\Csharp\UnitTestsGenerator\SampleLibrary\ClassC.cs");
            generatorConfig.EndpointFolder = @"D:\CODE\Csharp\UnitTestsGenerator\SampleLibrary.Tests\";
            
            await generator.GenerateTests(generatorConfig);
        }
    }
}
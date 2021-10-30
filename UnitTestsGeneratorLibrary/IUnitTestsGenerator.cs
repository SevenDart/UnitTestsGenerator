using System.Threading.Tasks;

namespace UnitTestsGeneratorLibrary
{
    public interface IUnitTestsGenerator
    {
        Task GenerateTests(GeneratorConfig generatorConfig);
    }
}
using System.Collections.Generic;

namespace UnitTestsGeneratorLibrary
{
    public class GeneratorConfig
    {
        public List<string> Filenames { get; set; } = new List<string>();
        
        public string EndpointFolder { get; set; }

        public int MaxParallelLoadCount { get; set; } = 1;
        public int MaxParallelWriteCount { get; set; } = 1;
        public int MaxParallelExecCount { get; set; } = 1;
    }
}
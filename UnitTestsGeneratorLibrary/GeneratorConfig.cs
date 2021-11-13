using System.Collections.Generic;

namespace UnitTestsGeneratorLibrary
{
    public class GeneratorConfig
    {
        public List<string> Filenames { get; set; } = new List<string>();
        
        public string EndpointFolder { get; set; }
        
        public int MaxParallelLoadCount { get; set; }
        public int MaxParallelWriteCount { get; set; }
        public int MaxParallelExecCount { get; set; }
    }
}
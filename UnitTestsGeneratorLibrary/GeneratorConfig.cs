using System.Collections.Generic;

namespace UnitTestsGeneratorLibrary
{
    public class GeneratorConfig
    {
        public List<string> Filenames { get; set; } = new List<string>();
        
        public string EndpointFolder { get; set; }
        
        public int MaxNumberOfLoadedFilesAtOnce { get; set; }
        
        public int MaxDegreeOfParallelism { get; set; }
    }
}
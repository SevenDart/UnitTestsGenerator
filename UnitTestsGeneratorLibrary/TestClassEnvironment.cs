using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using UnitTestsGeneratorLibrary.Models;

namespace UnitTestsGeneratorLibrary
{
    public class TestClassEnvironment
    {
        public string Filename { get; set; }
        
        public string SourceText { get; set; }
        
        public List<TestingClassModel> TestingClassModels { get; set; }
        
        public SyntaxTree GeneratedSyntaxTree { get; set; }
    }
}
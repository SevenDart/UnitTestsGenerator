using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnitTestsGeneratorLibrary.Models
{
    public class TestingMethodModel
    {
        public string MethodName { get; set; }
        
        public IEnumerable<ArgumentModel> Arguments { get; set; }

        public static IEnumerable<TestingMethodModel> GetPublicMethods(ClassDeclarationSyntax classDeclarationSyntax)
        {
            var methods = classDeclarationSyntax
                .ChildNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(method => method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword)));

            var methodList = new List<TestingMethodModel>();
            foreach (var method in methods)
            {
                var methodArguments = method.ParameterList.Parameters.Select(parameter => new ArgumentModel
                {
                    Name = parameter.Identifier.ToString(),
                    Type = parameter.Type.ToString()
                }).ToList();
                methodList.Add(new TestingMethodModel()
                {
                    MethodName = method.Identifier.ToString(),
                    Arguments = methodArguments
                });
            }

            return methodList;
        }
    }
}
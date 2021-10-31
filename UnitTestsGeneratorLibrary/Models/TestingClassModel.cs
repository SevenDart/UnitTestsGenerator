using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnitTestsGeneratorLibrary.Models
{
    public class TestingClassModel
    {
        public string ClassName { get; set; }

        public IEnumerable<TestingMethodModel> Methods { get; set; }
        
        public IEnumerable<ArgumentModel> ConstructorArguments { get; set; } 

        private static IEnumerable<ClassDeclarationSyntax> GetClassesDeclarations(SyntaxNode syntaxNode)
        {
            var result = new List<ClassDeclarationSyntax>();
            if (syntaxNode.IsKind(SyntaxKind.ClassDeclaration))
            {
                result.Add(syntaxNode as ClassDeclarationSyntax);
            }
            foreach (var node in syntaxNode.ChildNodes())
            {
                result.AddRange(GetClassesDeclarations(node));
            }
            return result;
        }

        private static IEnumerable<ArgumentModel> GetConstructorArguments(ClassDeclarationSyntax classDeclarationSyntax)
        {
            var constructors =  classDeclarationSyntax
                .ChildNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .OrderByDescending(
                    c => c.ParameterList.Parameters.Count
                    );
            
            return constructors
                .First()
                .ParameterList
                .Parameters
                .Select(parameter => new ArgumentModel
                {
                    Name = parameter.Identifier.ToString(),
                    Type = parameter.Type.ToString()
                }).ToList();
        }

        public static IEnumerable<TestingClassModel> ParseSyntaxNode(SyntaxNode syntaxNode)
        {
            var classes = GetClassesDeclarations(syntaxNode);

            var classList = new List<TestingClassModel>();
            foreach (var classDeclaration in classes)
            {
                var constructorArgs = GetConstructorArguments(classDeclaration);
                var methods = TestingMethodModel.GetPublicMethods(classDeclaration);
                classList.Add(new TestingClassModel()
                {
                    ClassName = classDeclaration.Identifier.ToString(),
                    ConstructorArguments = constructorArgs,
                    Methods = methods
                });
            }

            return classList;
        }
    }
}
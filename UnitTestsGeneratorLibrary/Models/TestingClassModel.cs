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

        public List<TestingMethodModel> Methods { get; set; }
        
        public List<ArgumentModel> ConstructorArguments { get; set; } 
        
        public List<string> UsedNamespaces { get; set; }

        private static List<ClassDeclarationSyntax> GetClassesDeclarations(SyntaxNode syntaxNode)
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

        private static List<ArgumentModel> GetConstructorArguments(ClassDeclarationSyntax classDeclarationSyntax)
        {
            var constructors =  classDeclarationSyntax
                .ChildNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .OrderByDescending(
                    c => c.ParameterList.Parameters.Count
                    )
                .ToList();

            if (constructors.Count != 0)
            {
                return constructors
                    .First()
                    .ParameterList
                    .Parameters
                    .Select(parameter => new ArgumentModel
                    {
                        Name = parameter.Identifier.ToString(),
                        ArgType = parameter.Type.ToString()
                    }).ToList();
            }

            return new List<ArgumentModel>();
        }

        private static List<string> FindUsedNamespaces(SyntaxNode syntaxNode)
        {
            var list = syntaxNode
                .ChildNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(nm => nm.Name.ToString())
                .ToList();

            var thisNamespace = syntaxNode.ChildNodes().OfType<NamespaceDeclarationSyntax>().SingleOrDefault();

            if (thisNamespace != null)
            {
                list.Add(thisNamespace.Name.ToString());
            }
            
            return list;
        }

        public static List<TestingClassModel> ParseSyntaxNode(SyntaxNode syntaxNode)
        {
            var classes = GetClassesDeclarations(syntaxNode);

            var namespaces = FindUsedNamespaces(syntaxNode);

            var classList = new List<TestingClassModel>();
            foreach (var classDeclaration in classes)
            {
                var constructorArgs = GetConstructorArguments(classDeclaration);
                var methods = TestingMethodModel.GetPublicMethods(classDeclaration);
                classList.Add(new TestingClassModel()
                {
                    ClassName = classDeclaration.Identifier.ToString(),
                    ConstructorArguments = constructorArgs,
                    Methods = methods,
                    UsedNamespaces = namespaces
                });
            }

            return classList;
        }
    }
}
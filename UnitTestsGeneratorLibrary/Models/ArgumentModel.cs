using System;
using System.Collections.Generic;

namespace UnitTestsGeneratorLibrary.Models
{
    public class ArgumentModel
    {
        public string ArgType { get; set; }

        public string Name { get; set; }

        private static readonly Dictionary<string, Type> PrimitiveTypes = new()
        {
            {"void", typeof(void)},
            {"byte", typeof(byte)},
            {"short", typeof(short)},
            {"int", typeof(int)},
            {"bool", typeof(bool)},
            {"double", typeof(double)},
            {"float", typeof(float)},
            {"string", typeof(string)},
            
        };

        private static readonly Dictionary<string, Type> StructTypes = new()
        {
            {"Decimal", typeof(Decimal)},
            {"DateTime", typeof(DateTime)},
            {"TimeSpan", typeof(TimeSpan)}
        };

        public Type TryToParseType()
        {
            PrimitiveTypes.TryGetValue(ArgType, out var type);
            if (type == null)
            {
                StructTypes.TryGetValue(ArgType, out type);
            }
            return type ?? typeof(object);
        }

        public static string GetDefaultValue(Type type)
        {
            if (type.IsValueType)
            {
                return PrimitiveTypes.ContainsValue(type) 
                    ? Activator.CreateInstance(type).ToString().ToLower() 
                    : $"new {type}()";
            }

            return "null";
        }
    }
}
using System;

namespace DebugCommandExecutor
{
    public static class TypeExtensions
    {
        public static string GetFriendlyName(this Type type)
        {
            if (type == typeof(int)) return "int";
            else if (type == typeof(uint)) return "uint";
            else if (type == typeof(short)) return "short";
            else if (type == typeof(ushort)) return "ushort";
            else if (type == typeof(byte)) return "byte";
            else if (type == typeof(sbyte)) return "sbyte";
            else if (type == typeof(bool)) return "bool";
            else if (type == typeof(long)) return "long";
            else if (type == typeof(ulong)) return "ulong";
            else if (type == typeof(float)) return "float";
            else if (type == typeof(double)) return "double";
            else if (type == typeof(decimal)) return "decimal";
            else if (type == typeof(string)) return "string";
            else return type.Name;
        }
    }
}

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
#else
using System.Diagnostics;
#endif

namespace DebugCommandExecutor
{
    public static class DebugCommand
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly IReadOnlyDictionary<string, DebugMethod> DebugMethods;

        static DebugCommand()
        {
            DebugMethods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Select(x =>
                {
                    var attribute = x.GetCustomAttribute<DebugCommandAttribute>();
                    return attribute == null ? null : new DebugMethod(x, attribute);
                })
                .Where(x => x != null)
                .ToDictionary(x => x.MethodInfo.Name.ToLowerInvariant(), x => x);
        }

        public static void Execute(string text)
        {
            var input = ParseString(text);
            if (input.Length == 0) return;

            var commandName = input[0].ToLowerInvariant();
            if (!DebugMethods.TryGetValue(commandName, out var debugMethod))
            {
                UnityEngine.Debug.LogWarning($"DebugCommand | DebugCommand({commandName}) is not found\ninput: {text}");
                return;
            }

            var parameterInfos = debugMethod.MethodInfo.GetParameters();
            if (parameterInfos.Length != input.Length - 1)
            {
                UnityEngine.Debug.LogWarning($"DebugCommand | DebugCommand({debugMethod.MethodInfo.Name}) needs {parameterInfos.Length} parameters ({string.Join(", ", parameterInfos.Select(x => $"{x.ParameterType.Name} {x.Name}"))})\ninput: {text}");
                return;
            }

            var convertedArguments = new object[parameterInfos.Length];
            for (var i = 0; i < parameterInfos.Length; i++)
            {
                var parameterInfo = parameterInfos[i];
                var value = input[i + 1];
                var targetType = parameterInfo.ParameterType;
                try
                {
                    if (targetType.IsEnum)
                    {
                        if (int.TryParse(value, out var intValue))
                        {
                            convertedArguments[i] = Enum.ToObject(targetType, intValue);
                            continue;
                        }

                        if (Enum.TryParse(targetType, value, out var enumValue))
                        {
                            convertedArguments[i] = enumValue;
                            continue;
                        }
                    }

                    convertedArguments[i] = Convert.ChangeType(value, targetType);
                }
                catch (FormatException formatException)
                {
                    UnityEngine.Debug.LogWarning($"DebugCommand | Parse Error \"{value}\" to {targetType.Name} ({formatException.Message})\ninput: {text}");
                }
                catch (InvalidCastException invalidCastException)
                {
                    UnityEngine.Debug.LogWarning($"DebugCommand | Parse Error \"{value}\" to {targetType.Name} ({invalidCastException.Message})\ninput: {text}");
                }
            }

            debugMethod.MethodInfo.Invoke(null, convertedArguments);
        }

        private static string[] ParseString(string input)
        {
            var result = new List<string>();
            var inQuotes = false;
            var currentElement = new StringBuilder();

            foreach (var c in input)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (!inQuotes && char.IsWhiteSpace(c))
                {
                    if (currentElement.Length == 0) continue;
                    result.Add(currentElement.ToString());
                    currentElement.Clear();
                }
                else
                {
                    currentElement.Append(c);
                }
            }

            if (currentElement.Length > 0)
            {
                result.Add(currentElement.ToString());
            }

            return result.ToArray();
        }

        private class DebugMethod
        {
            public MethodInfo MethodInfo { get; }
            public DebugCommandAttribute Attribute { get; }

            public DebugMethod(MethodInfo methodInfo, DebugCommandAttribute attribute)
            {
                MethodInfo = methodInfo;
                Attribute = attribute;
            }
        }
#else
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Execute(string text)
        {
        }
#endif
    }
}

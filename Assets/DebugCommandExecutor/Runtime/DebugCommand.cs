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
        public static readonly IReadOnlyDictionary<string, DebugMethod> DebugMethods;

        static DebugCommand()
        {
            var targetAssemblyName = typeof(DebugCommandAttribute).Assembly.GetName().FullName;
            DebugMethods = AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => x.GetReferencedAssemblies().Any(y => y.FullName == targetAssemblyName))
                .SelectMany(x => x.GetTypes())
                .SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Select(x => new { Method = x, Attribute = x.GetCustomAttribute<DebugCommandAttribute>() })
                .Where(x => x.Attribute != null)
                .ToDictionary(
                    x => x.Method.Name.ToLowerInvariant(),
                    x => new DebugMethod(x.Method, x.Attribute)
                );
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
                UnityEngine.Debug.LogWarning($"DebugCommand | DebugCommand({debugMethod.MethodInfo.Name}) needs {parameterInfos.Length} parameters ({string.Join(", ", parameterInfos.Select(x => $"{x.ParameterType.GetFriendlyName()} {x.Name}"))})\ninput: {text}");
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

                        if (Enum.TryParse(targetType, value, true, out var enumValue))
                        {
                            convertedArguments[i] = enumValue;
                            continue;
                        }
                    }

                    convertedArguments[i] = Convert.ChangeType(value, targetType);
                }
                catch (FormatException formatException)
                {
                    UnityEngine.Debug.LogWarning($"DebugCommand | Parse Error \"{value}\" to {targetType.GetFriendlyName()} ({formatException.Message})\ninput: {text}");
                }
                catch (InvalidCastException invalidCastException)
                {
                    UnityEngine.Debug.LogWarning($"DebugCommand | Parse Error \"{value}\" to {targetType.GetFriendlyName()} ({invalidCastException.Message})\ninput: {text}");
                }
            }

            try
            {
                debugMethod.MethodInfo.Invoke(null, convertedArguments);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
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

        public class DebugMethod
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

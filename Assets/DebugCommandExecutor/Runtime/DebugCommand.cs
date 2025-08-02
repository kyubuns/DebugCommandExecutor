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
        public static readonly IReadOnlyDictionary<string, List<DebugMethod>> DebugMethods;

        static DebugCommand()
        {
            var targetAssemblyName = typeof(DebugCommandAttribute).Assembly.FullName;
            var debugMethods = new Dictionary<string, List<DebugMethod>>(StringComparer.OrdinalIgnoreCase);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.GetReferencedAssemblies().Any(x => string.Equals(x.FullName, targetAssemblyName, StringComparison.Ordinal))) continue;

                foreach (var type in assembly.GetTypes())
                {
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    {
                        var attribute = method.GetCustomAttribute<DebugCommandAttribute>();
                        if (attribute == null) continue;

                        var debugMethod = new DebugMethod(method, attribute);
                        if (!debugMethods.TryGetValue(method.Name, out var list))
                        {
                            list = new List<DebugMethod>();
                            debugMethods[method.Name] = list;
                        }

                        list.Add(debugMethod);
                    }
                }
            }

            DebugMethods = debugMethods;
        }

        public static void Execute(string text)
        {
            var input = ParseString(text);
            if (input.Length == 0) return;

            var commandName = input[0];
            if (!DebugMethods.TryGetValue(commandName, out var debugMethodList))
            {
                UnityEngine.Debug.LogWarning($"DebugCommand | DebugCommand({commandName}) is not found\ninput: {text}");
                return;
            }

            var targets = debugMethodList.Where(x => x.MethodInfo.GetParameters().Length == input.Length - 1).ToList();
            if (targets.Count == 0)
            {
                var debugMethod = debugMethodList[0];
                var parameterInfos = debugMethod.MethodInfo.GetParameters();
                UnityEngine.Debug.LogWarning($"DebugCommand | DebugCommand({debugMethod.MethodInfo.Name}) needs {parameterInfos.Length} parameters ({HumanReadableArguments(parameterInfos)})\ninput: {text}");
                return;
            }

            foreach (var debugMethod in targets)
            {
                var parameterInfos = debugMethod.MethodInfo.GetParameters();
                var convertedArguments = new object[parameterInfos.Length];

                for (var i = 0; i < parameterInfos.Length; i++)
                {
                    string value;
                    var parameterInfo = parameterInfos[i];
                    if (i + 1 < input.Length)
                    {
                        value = input[i + 1];
                    }
                    else if (parameterInfo.HasDefaultValue)
                    {
                        convertedArguments[i] = parameterInfo.DefaultValue;
                        continue;
                    }
                    else
                    {
                        throw new ArgumentException($"DebugCommand({debugMethod.MethodInfo.Name}) needs {parameterInfos.Length} parameters ({HumanReadableArguments(parameterInfos)})\ninput: {text}");
                    }

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

                return;
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

        public static string HumanReadableArguments(ParameterInfo[] parameterInfos)
        {
            return string.Join(", ", parameterInfos.Select(x =>
            {
                if (x.HasDefaultValue)
                {
                    return $"{x.ParameterType.GetFriendlyName()} {x.Name} = {x.DefaultValue}";
                }
                else
                {
                    return $"{x.ParameterType.GetFriendlyName()} {x.Name}";
                }
            }));
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

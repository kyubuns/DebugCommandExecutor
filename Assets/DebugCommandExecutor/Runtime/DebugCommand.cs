using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

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
                    if (attribute == null) return null;
                    return new DebugMethod(x, attribute);
                })
                .Where(x => x != null)
                .ToDictionary(x => x.MethodInfo.Name.ToLowerInvariant(), x => x);
        }

        public static bool Execute(string text)
        {
            var input = ParseString(text);
            if (input.Length == 0) return false;

            var commandName = input[0].ToLowerInvariant();
            if (!DebugMethods.TryGetValue(commandName, out var debugMethod))
            {
                UnityEngine.Debug.LogWarning($"DebugCommand | DebugCommand({commandName}) is not found\ninput: {text}");
                return false;
            }

            var parameterInfos = debugMethod.MethodInfo.GetParameters();
            if (parameterInfos.Length != input.Length - 1)
            {
                UnityEngine.Debug.LogWarning($"DebugCommand | DebugCommand({debugMethod.MethodInfo.Name}) needs {parameterInfos.Length} parameters ({string.Join(", ", parameterInfos.Select(x => $"{x.ParameterType.Name} {x.Name}"))})\ninput: {text}");
                return false;
            }

            var convertedArguments = new object[parameterInfos.Length];
            for (var i = 0; i < parameterInfos.Length; i++)
            {
                var parameterInfo = parameterInfos[i];
                var value = input[i + 1];
                try
                {
                    convertedArguments[i] = Convert.ChangeType(value, parameterInfo.ParameterType);
                }
                catch (FormatException e)
                {
                    UnityEngine.Debug.LogWarning($"DebugCommand | Parse Error \"{value}\" to {parameterInfo.ParameterType.Name} ({e.Message})\ninput: {text}");
                }
            }

            debugMethod.MethodInfo.Invoke(null, convertedArguments);

            return true;
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
        public static void Execute(string text)
        {
            UnityEngine.Debug.LogWarning("DebugCommand.Execute is called but it's not available in the release build");
        }
#endif
    }
}

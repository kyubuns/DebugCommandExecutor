using System.Diagnostics;

namespace DebugCommandExecutor
{
    public static class DebugCommand
    {
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Execute(string text)
        {
            UnityEngine.Debug.Log($"DebugCommand.Execute({text})");
        }
    }
}

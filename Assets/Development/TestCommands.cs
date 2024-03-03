using DebugCommandExecutor;

namespace Sandbox
{
    public static class TestCommands
    {
        [DebugCommand("Echo Text")]
        public static void Echo(string text)
        {
            UnityEngine.Debug.Log(text);
        }
    }
}

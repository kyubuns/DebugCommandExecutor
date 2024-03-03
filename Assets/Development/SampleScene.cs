using DebugCommandExecutor;
using UnityEngine;

namespace Sandbox
{
    public class SampleScene : MonoBehaviour
    {
        public void Start()
        {
            DebugCommand.Execute("Echo \"Test Message\"");
            DebugCommand.Execute("SpawnCube");
            DebugCommand.Execute("SetCubeColor 255 0 0");
            DebugCommand.Execute("SetCubePosition 1 0 0");
            DebugCommand.Execute("EchoEnum 2");
            DebugCommand.Execute("EchoEnum Blue");
        }
    }
}

using DebugCommandExecutor;
using UnityEngine;

namespace Sandbox
{
    public static class TestCommands
    {
        [DebugCommand("Show application version")]
        public static void ApplicationVersion()
        {
            Debug.Log($"ApplicationVersion is {Application.version}");
        }

        [DebugCommand("Echo text")]
        public static void Echo(string text)
        {
            Debug.Log(text);
        }

        [DebugCommand]
        public static void Add(int a, int b)
        {
            Debug.Log($"{a} + {b} = {a + b}");
        }

        [DebugCommand]
        public static void SpawnCube()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Cube";

            Debug.Log("Spawn Cube Success!");
        }

        [DebugCommand]
        public static void SetCubeColor(int r, int g, int b)
        {
            var cube = GameObject.Find("Cube");
            cube.GetComponent<MeshRenderer>().material.color = new Color32((byte) r, (byte) g, (byte) b, 255);

            Debug.Log($"Set Cube Color ({r}, {g}, {b})");
        }

        [DebugCommand]
        public static void SetCubePosition(float x, float y, float z)
        {
            var cube = GameObject.Find("Cube");
            cube.transform.position = new Vector3(x, y, z);

            Debug.Log($"Set Cube Position ({x}, {y}, {z})");
        }
    }
}

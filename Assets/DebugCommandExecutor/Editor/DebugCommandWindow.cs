using UnityEditor;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;

namespace DebugCommandExecutor.Editor
{
    public class DebugCommandWindow : EditorWindow
    {
        [MenuItem("Tools/Debug Command Executor/Window")]
        private static void ShowWindow()
        {
            var window = GetWindow<DebugCommandWindow>();
            window.titleContent = new GUIContent("Debug Command Window");
            window.Show();
        }

        private string _message = "";

        private void OnEnable()
        {
            EditorConnection.instance.Initialize();
        }

        private void OnDisable()
        {
            EditorConnection.instance.DisconnectAll();
        }

        private void OnGUI()
        {
            _message = EditorGUILayout.TextField("Message:", _message);

            if (GUILayout.Button("Send to Player"))
            {
                SendToPlayer(_message);
            }

            if (GUILayout.Button("Send to Editor"))
            {
                SendToEditor(_message);
            }
        }

        private void SendToPlayer(string message)
        {
            var connection = EditorConnection.instance;
            var bytes = System.Text.Encoding.UTF8.GetBytes(message);
            connection.Send(RemoteExecutor.DebugCommandMessage, bytes);
        }

        private void SendToEditor(string message)
        {
            DebugCommand.Execute(message);
        }
    }
}

using System.Linq;
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

        private int _recipient;
        private string _message;

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
            _recipient = EditorGUILayout.Popup(_recipient, new[] { "Editor", "Player" });
            _message = EditorGUILayout.TextField("Message:", _message);

            if (_recipient == 0)
            {
                if (Application.isPlaying)
                {
                    if (GUILayout.Button("Send to Editor"))
                    {
                        SendToEditor(_message);
                    }
                }
                else
                {
                    using (_ = new EditorGUI.DisabledGroupScope(true))
                    {
                        GUILayout.Button("Send to Editor (Not playing)");
                    }
                }
            }
            else
            {
                var connection = EditorConnection.instance;
                if (connection.ConnectedPlayers.Count > 0)
                {
                    if (GUILayout.Button($"Send to Player ({string.Join(", ", connection.ConnectedPlayers.Select(x => x.name))})"))
                    {
                        SendToPlayer(_message);
                    }
                }
                else
                {
                    using (_ = new EditorGUI.DisabledGroupScope(true))
                    {
                        GUILayout.Button("Send to Player (ConnectedPlayers.Count == 0)");
                    }
                }
            }
        }

        private void SendToPlayer(string message)
        {
            var connection = EditorConnection.instance;
            if (connection.ConnectedPlayers.Count == 0)
            {
                Debug.LogWarning($"DebugCommand | connection.ConnectedPlayers.Count == 0");
                return;
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(message);
            connection.Send(RemoteExecutor.DebugCommandMessage, bytes);
        }

        private void SendToEditor(string message)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"DebugCommand | !Application.isPlaying");
                return;
            }

            DebugCommand.Execute(message);
        }
    }
}

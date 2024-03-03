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
        private bool _refocus;

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

            GUI.SetNextControlName("MessageTextField");
            _message = EditorGUILayout.TextField(_message);

            if (_refocus)
            {
                GUI.FocusControl("MessageTextField");
                _refocus = false;
            }

            if (_recipient == 0)
            {
                if (Application.isPlaying)
                {
                    if (GUILayout.Button("Send to Editor"))
                    {
                        Send();
                    }
                }
                else
                {
                    using (_ = new EditorGUI.DisabledGroupScope(true))
                    {
                        GUILayout.Button("Send to Editor (not playing)");
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
                        Send();
                    }
                }
                else
                {
                    using (_ = new EditorGUI.DisabledGroupScope(true))
                    {
                        GUILayout.Button("Send to Player (no players)");
                    }
                }
            }

            var e = Event.current;
            if ((e.control || e.command) && e.type == EventType.KeyDown && e.keyCode == KeyCode.Return)
            {
                Send();
                e.Use();
            }
        }

        private void Send()
        {
            if (string.IsNullOrWhiteSpace(_message)) return;

            bool used;
            if (_recipient == 0)
            {
                used = SendToEditor(_message);
            }
            else
            {
                used = SendToPlayer(_message);
            }

            if (used)
            {
                _message = string.Empty;
                _refocus = true;
                GUI.FocusControl(null); // 一回フォーカスを外さないとTextAreaが正しくリセットされない
                Repaint();
            }
        }

        private static bool SendToPlayer(string message)
        {
            var connection = EditorConnection.instance;
            if (connection.ConnectedPlayers.Count == 0) return false;

            var bytes = System.Text.Encoding.UTF8.GetBytes(message);
            connection.Send(RemoteExecutor.DebugCommandMessage, bytes);

            return true;
        }

        private static bool SendToEditor(string message)
        {
            if (!Application.isPlaying) return false;

            DebugCommand.Execute(message);

            return true;
        }
    }
}

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
            var send = false;

            // event
            {
                var e = Event.current;
                if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return)
                {
                    GUI.FocusControl(null);
                    Send();
                    send = true;
                }
            }

            // gui
            {
                var validate = Validate();

                using (new EditorGUILayout.HorizontalScope())
                {
                    _recipient = GUILayout.SelectionGrid(_recipient, new[] { "Editor", "Player" }, 2, GUILayout.MaxWidth(100));

                    if (string.IsNullOrEmpty(validate))
                    {
                        GUI.SetNextControlName("MessageTextField");
                        _message = EditorGUILayout.TextField(_message);
                    }
                    else
                    {
                        using (new EditorGUI.DisabledGroupScope(true))
                        {
                            EditorGUILayout.TextField(validate);
                        }
                    }
                }
            }

            // event
            {
                if (send)
                {
                    GUI.FocusControl("MessageTextField");
                }
            }
        }

        private string Validate()
        {
            if (_recipient == 0)
            {
                return ValidateSendToEditor();
            }
            else
            {
                return ValidateSendToPlayer();
            }
        }

        private void Send()
        {
            if (string.IsNullOrWhiteSpace(_message)) return;
            if (!string.IsNullOrEmpty(Validate())) return;

            if (_recipient == 0)
            {
                SendToEditor(_message);
            }
            else
            {
                SendToPlayer(_message);
            }

            _message = string.Empty;
        }

        private string ValidateSendToEditor()
        {
            if (!Application.isPlaying) return "not playing";
            return null;
        }

        private static void SendToEditor(string message)
        {
            DebugCommand.Execute(message);
        }

        private string ValidateSendToPlayer()
        {
            var connection = EditorConnection.instance;
            if (connection.ConnectedPlayers.Count == 0) return "no player";
            return null;
        }

        private static void SendToPlayer(string message)
        {
            var connection = EditorConnection.instance;
            var bytes = System.Text.Encoding.UTF8.GetBytes(message);
            connection.Send(RemoteExecutor.DebugCommandMessage, bytes);
        }
    }
}

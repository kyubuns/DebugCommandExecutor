using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;

namespace DebugCommandExecutor.Editor
{
    public class DebugCommandWindow : EditorWindow
    {
        [MenuItem("Tools/Debug Command Executor/Window")]
        protected static void ShowWindow()
        {
            var window = GetWindow<DebugCommandWindow>();
            window.titleContent = new GUIContent("Debug Command Window");
            window.Show();
        }

        private const int HistoryMax = 30;
        private static string EditorPrefsHistoryKey => $"DebugCommand.{Application.productName}";

        private List<string> _history;
        private int _recipient;
        private string _message;
        private bool _refocusNextFrame;
        private int _focusHistory = -1;
        private Vector2 _historyScrollPosition;

        protected void OnEnable()
        {
            EditorConnection.instance.Initialize();
        }

        protected void OnDisable()
        {
            EditorConnection.instance.DisconnectAll();
        }

        protected void Awake()
        {
            _history = EditorPrefs.GetString(EditorPrefsHistoryKey)
                .Split("\n")
                .ToList();
        }

        protected void OnGUI()
        {
            var refocus = false;

            {
                if (_refocusNextFrame)
                {
                    _refocusNextFrame = false;
                    refocus = true;
                }
            }

            // event
            {
                var e = Event.current;
                if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return)
                {
                    GUI.FocusControl(null);
                    Send();
                    refocus = true;
                }

                if (e.type == EventType.KeyDown && e.keyCode == KeyCode.UpArrow)
                {
                    if (_focusHistory + 1 < _history.Count)
                    {
                        _focusHistory += 1;

                        GUI.FocusControl(null);
                        _message = _history[_focusHistory];
                        _refocusNextFrame = true;
                    }
                }

                if (e.type == EventType.KeyDown && e.keyCode == KeyCode.DownArrow)
                {
                    if (_focusHistory - 1 >= 0)
                    {
                        _focusHistory -= 1;

                        GUI.FocusControl(null);
                        _message = _history[_focusHistory];
                        _refocusNextFrame = true;
                    }
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
                        var prevMessage = _message;
                        GUI.SetNextControlName("MessageTextField");
                        _message = EditorGUILayout.TextField(_message);

                        if (_message != prevMessage) _focusHistory = -1;
                    }
                    else
                    {
                        GUI.FocusControl(null);
                        using (new EditorGUI.DisabledGroupScope(true))
                        {
                            EditorGUILayout.TextField(validate);
                        }
                    }
                }

                EditorGUILayout.Space();

                using (var scrollView = new EditorGUILayout.ScrollViewScope(_historyScrollPosition))
                {
                    EditorGUILayout.LabelField("History");
                    for (var i = 0; i < _history.Count; i++)
                    {
                        var history = _history[i];
                        if (GUILayout.Button(history))
                        {
                            GUI.FocusControl(null);
                            _message = history;
                            _refocusNextFrame = true;
                            _focusHistory = i;
                        }
                    }

                    _historyScrollPosition = scrollView.scrollPosition;
                }
            }

            // event
            {
                if (_refocusNextFrame)
                {
                    Repaint();
                }
                else if (refocus)
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
            var message = _message.Trim();

            if (string.IsNullOrWhiteSpace(message)) return;
            if (!string.IsNullOrEmpty(Validate())) return;

            if (_recipient == 0)
            {
                SendToEditor(message);
            }
            else
            {
                SendToPlayer(message);
            }

            SaveHistory(message);

            _message = string.Empty;
            _focusHistory = -1;
            _historyScrollPosition = Vector2.zero;
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

        private void SaveHistory(string newMessage)
        {
            _history.Remove(newMessage);
            _history.Insert(0, newMessage);

            while (_history.Count > HistoryMax)
            {
                _history.RemoveAt(_history.Count - 1);
            }

            EditorPrefs.SetString(EditorPrefsHistoryKey, string.Join("\n", _history));
        }
    }
}

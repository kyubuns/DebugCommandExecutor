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
        private const int AutocompleteMinLength = 2;
        private static string EditorPrefsHistoryKey => $"DebugCommand.{Application.productName}";

        private List<string> _history;

        private int _recipient;
        private string _text;
        private bool _refocusNextFrame;
        private int _focusHistory = -1;
        private int _focusAutocomplete = -1;
        private Vector2 _autocompleteScrollPosition;
        private Vector2 _historyScrollPosition;
        private IReadOnlyList<DebugCommand.DebugMethod> _autoCompleteMethods = new List<DebugCommand.DebugMethod>();

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
            var targetButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fixedHeight = 22,
                fixedWidth = 80,
            };

            var messageTextFieldStyle = new GUIStyle(EditorStyles.textField)
            {
                fontSize = 16,
                fixedHeight = 22,
            };

            var refocus = false;

            {
                if (_refocusNextFrame && Event.current.type == EventType.Repaint)
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
                    e.Use();

                    GUI.FocusControl(null);
                    if (0 <= _focusHistory - 1 && _focusHistory - 1 < _history.Count)
                    {
                        _focusHistory -= 1;
                        _text = _history[_focusHistory];
                    }

                    _refocusNextFrame = true;
                }
                else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.DownArrow)
                {
                    e.Use();

                    GUI.FocusControl(null);
                    if (_focusHistory + 1 < _history.Count)
                    {
                        _focusHistory += 1;
                        _text = _history[_focusHistory];
                    }

                    _refocusNextFrame = true;
                }

                if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Tab && e.shift)
                {
                    e.Use();

                    GUI.FocusControl(null);
                    if (0 <= _focusAutocomplete - 1 && _focusAutocomplete - 1 < _autoCompleteMethods.Count)
                    {
                        _focusAutocomplete -= 1;
                        var autoCompleteMethod = _autoCompleteMethods[_focusAutocomplete];
                        var parameterInfos = autoCompleteMethod.MethodInfo.GetParameters();
                        _text = autoCompleteMethod.MethodInfo.Name + (parameterInfos.Length > 0 ? " " : "");
                    }

                    _refocusNextFrame = true;
                }
                else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Tab)
                {
                    e.Use();

                    GUI.FocusControl(null);
                    if (_focusAutocomplete + 1 < _autoCompleteMethods.Count)
                    {
                        _focusAutocomplete += 1;
                        var autoCompleteMethod = _autoCompleteMethods[_focusAutocomplete];
                        var parameterInfos = autoCompleteMethod.MethodInfo.GetParameters();
                        _text = autoCompleteMethod.MethodInfo.Name + (parameterInfos.Length > 0 ? " " : "");
                    }

                    _refocusNextFrame = true;
                }
            }

            // gui
            {
                var validate = Validate();

                using (new EditorGUILayout.HorizontalScope())
                {
                    _recipient = GUILayout.SelectionGrid(_recipient, new[] { "Editor", "Player" }, 2, targetButtonStyle);

                    if (string.IsNullOrEmpty(validate))
                    {
                        var prevText = _text;
                        GUI.SetNextControlName("MessageTextField");
                        _text = EditorGUILayout.TextField(_text, messageTextFieldStyle);

                        if (_text != prevText)
                        {
                            _focusHistory = -1;
                            _focusAutocomplete = -1;
                            _autoCompleteMethods = UpdateAutoComplete(_text);
                            _focusAutocomplete = -1;
                        }
                    }
                    else
                    {
                        GUI.FocusControl(null);
                        using (new EditorGUI.DisabledGroupScope(true))
                        {
                            EditorGUILayout.TextField(validate, messageTextFieldStyle);
                        }
                    }
                }

                EditorGUILayout.Space();

                if (_autoCompleteMethods.Count > 0)
                {
                    // AutoComplete
                    EditorGUILayout.LabelField("Autocomplete");
                    using (var scrollView = new EditorGUILayout.ScrollViewScope(_autocompleteScrollPosition))
                    {
                        for (var i = 0; i < _autoCompleteMethods.Count; i++)
                        {
                            var autoCompleteMethod = _autoCompleteMethods[i];
                            var methodName = autoCompleteMethod.MethodInfo.Name;
                            var parameterInfos = autoCompleteMethod.MethodInfo.GetParameters();
                            var text = parameterInfos.Length == 0
                                ? methodName
                                : $"{methodName}({string.Join(", ", parameterInfos.Select(x => $"{x.ParameterType.GetFriendlyName()} {x.Name}"))})";

                            if (!string.IsNullOrEmpty(autoCompleteMethod.Attribute.Summary))
                            {
                                text += $" - {autoCompleteMethod.Attribute.Summary}";
                            }

                            if (GUILayout.Button(text))
                            {
                                GUI.FocusControl(null);
                                _text = methodName + (parameterInfos.Length > 0 ? " " : "");
                                _refocusNextFrame = true;
                            }
                        }

                        _autocompleteScrollPosition = scrollView.scrollPosition;
                    }
                }
                else
                {
                    // History
                    EditorGUILayout.LabelField("History");
                    using (var scrollView = new EditorGUILayout.ScrollViewScope(_historyScrollPosition))
                    {
                        for (var i = 0; i < _history.Count; i++)
                        {
                            var history = _history[i];
                            if (GUILayout.Button(history))
                            {
                                GUI.FocusControl(null);
                                _text = history;
                                _refocusNextFrame = true;
                                _focusHistory = i;
                            }
                        }

                        _historyScrollPosition = scrollView.scrollPosition;
                    }
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

        private static IReadOnlyList<DebugCommand.DebugMethod> UpdateAutoComplete(string text)
        {
            var methodName = text.Split(' ')[0].Trim().ToLowerInvariant();

            if (methodName.Length < AutocompleteMinLength)
            {
                return new List<DebugCommand.DebugMethod>();
            }

            return DebugCommand.DebugMethods
                .Where(x => x.Key.StartsWith(methodName))
                .Select(x => x.Value)
                .ToList();
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
            var message = _text.Trim();

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

            _text = string.Empty;
            _focusHistory = -1;
            _historyScrollPosition = Vector2.zero;
            _autoCompleteMethods = new List<DebugCommand.DebugMethod>();
            _focusAutocomplete = -1;
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;

namespace DebugCommandExecutor.Editor
{
    public class DebugCommandWindow : EditorWindow
    {
        [MenuItem("Tools/Debug Command/Window")]
        protected static void ShowWindow()
        {
            var window = GetWindow<DebugCommandWindow>();
            window.titleContent = new GUIContent("Debug Command Window");
            window.Show();
        }

        private const int HistoryMax = 30;
        private const int AutocompleteMinLength = 2;
        private static string EditorPrefsHistoryKey => $"DebugCommand.{Application.productName}";
        private static readonly Dictionary<string, ILookup<string, DebugCommand.DebugMethod>> AutocompleteCache = new(StringComparer.OrdinalIgnoreCase);

        private List<string> _history;
        private TextEditor _textEditor;

        private int _recipient;
        private string _text;
        private bool _refocusNextFrame;
        private int _focusHistory = -1;
        private int _focusAutocomplete = -1;
        private Vector2 _autocompleteScrollPosition;
        private IReadOnlyList<DebugCommand.DebugMethod> _autoCompleteMethods = new List<DebugCommand.DebugMethod>();
        private string _inputMethodName;

        protected void OnEnable()
        {
            EditorConnection.instance.Initialize();

            var editorField = typeof(EditorGUI).GetField("s_RecycledEditor", BindingFlags.Static | BindingFlags.NonPublic);
            _textEditor = editorField?.GetValue(null) as TextEditor;
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
                fixedHeight = 26,
                fixedWidth = 80,
            };

            var messageTextFieldStyle = new GUIStyle(EditorStyles.textField)
            {
                fontSize = 16,
                fixedHeight = 26,
            };

            var autocompleteButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fixedHeight = 20,
            };

            var autocompleteSelectingButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fixedHeight = 20,
                fontStyle = FontStyle.Bold,
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
                if (e.type == EventType.KeyUp && e.keyCode == KeyCode.Return)
                {
                    e.Use();

                    GUI.FocusControl(null);
                    Send();
                    refocus = true;
                }

                if (e.type == EventType.KeyDown && e.keyCode == KeyCode.DownArrow)
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
                else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.UpArrow)
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
                    if (_autoCompleteMethods.Count > 0)
                    {
                        _focusAutocomplete = _focusAutocomplete > 0 ? _focusAutocomplete - 1 : _autoCompleteMethods.Count - 1;
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
                    if (_autoCompleteMethods.Count > 0)
                    {
                        _focusAutocomplete = _focusAutocomplete + 1 < _autoCompleteMethods.Count ? _focusAutocomplete + 1 : 0;
                        var autoCompleteMethod = _autoCompleteMethods[_focusAutocomplete];
                        var parameterInfos = autoCompleteMethod.MethodInfo.GetParameters();
                        _text = autoCompleteMethod.MethodInfo.Name + (parameterInfos.Length > 0 ? " " : "");
                    }

                    _refocusNextFrame = true;
                }

                if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
                {
                    e.Use();

                    FocusGameWindowIfNeed();
                }
            }

            // gui
            {
                var validate = Validate();
                var prevText = _text;

                using (new EditorGUILayout.HorizontalScope())
                {
                    _recipient = GUILayout.SelectionGrid(_recipient, new[] { "Editor", "Player" }, 2, targetButtonStyle);

                    if (string.IsNullOrEmpty(validate))
                    {
                        var userPrevInput = _text;
                        GUI.SetNextControlName("MessageTextField");
                        _text = EditorGUILayout.TextField(_text, messageTextFieldStyle);
                        if (_text != userPrevInput)
                        {
                            _focusHistory = -1;
                            _focusAutocomplete = -1;
                        }
                    }
                    else
                    {
                        GUI.FocusControl(null);
                        _text = string.Empty;
                        using (new EditorGUI.DisabledGroupScope(true))
                        {
                            EditorGUILayout.TextField(validate, messageTextFieldStyle);
                        }
                    }
                }

                if (_text != prevText)
                {
                    _autoCompleteMethods = UpdateAutoComplete(_text);
                    _inputMethodName = _text.Split(' ')[0];
                }

                if (_autoCompleteMethods.Count > 0)
                {
                    EditorGUILayout.Space();

                    // AutoComplete
                    using (var scrollView = new EditorGUILayout.ScrollViewScope(_autocompleteScrollPosition))
                    {
                        for (var i = 0; i < _autoCompleteMethods.Count; i++)
                        {
                            var autoCompleteMethod = _autoCompleteMethods[i];
                            var methodName = autoCompleteMethod.MethodInfo.Name;
                            var parameterInfos = autoCompleteMethod.MethodInfo.GetParameters();
                            var text = $"{methodName}({DebugCommand.HumanReadableArguments(parameterInfos)})";

                            if (!string.IsNullOrEmpty(autoCompleteMethod.Attribute.Summary))
                            {
                                text += $" - {autoCompleteMethod.Attribute.Summary}";
                            }

                            var isFocused = (i == _focusAutocomplete) || (_focusAutocomplete == -1 && string.Equals(methodName, _inputMethodName, StringComparison.InvariantCultureIgnoreCase));
                            if (GUILayout.Button(text, isFocused ? autocompleteSelectingButtonStyle : autocompleteButtonStyle))
                            {
                                GUI.FocusControl(null);
                                _text = methodName + (parameterInfos.Length > 0 ? " " : "");
                                _refocusNextFrame = true;
                            }
                        }

                        _autocompleteScrollPosition = scrollView.scrollPosition;
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

                    if (_textEditor != null && _text != null)
                    {
                        _textEditor.cursorIndex = _text.Length;
                        _textEditor.selectIndex = _text.Length;
                    }
                }
            }
        }

        private static IReadOnlyList<DebugCommand.DebugMethod> UpdateAutoComplete(string text)
        {
            var methodName = text.Split(' ')[0].Trim();

            if (methodName.Length < AutocompleteMinLength)
            {
                return new List<DebugCommand.DebugMethod>();
            }

            var start = methodName.Substring(0, AutocompleteMinLength);
            if (!AutocompleteCache.TryGetValue(start, out var cache))
            {
                cache = DebugCommand.DebugMethods
                    .Where(x => x.Key.Contains(start, StringComparison.OrdinalIgnoreCase))
                    .SelectMany(x => x.Value.Select(y => (x.Key, Value: y)))
                    .ToLookup(
                        x => GetSubstringAfter(x.Key, start),
                        x => x.Value
                    );

                AutocompleteCache[start] = cache;
            }

            if (methodName.Length == AutocompleteMinLength)
            {
                return Sort(cache);
            }

            var remain = methodName.Substring(AutocompleteMinLength, methodName.Length - AutocompleteMinLength);
            return Sort(cache.Where(x => x.Key.StartsWith(remain, StringComparison.OrdinalIgnoreCase)));

            IReadOnlyList<DebugCommand.DebugMethod> Sort(IEnumerable<IGrouping<string, DebugCommand.DebugMethod>> elements)
            {
                return elements
                    .SelectMany(x => x)
                    .OrderBy(x => x.MethodInfo.Name.StartsWith(text, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .ToList();
            }
        }

        private static string GetSubstringAfter(string target, string searchString)
        {
            var index = target.IndexOf(searchString, StringComparison.OrdinalIgnoreCase);
            if (index != -1)
            {
                return target.Substring(index + searchString.Length);
            }

            return string.Empty;
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
            _autoCompleteMethods = new List<DebugCommand.DebugMethod>();
            _focusAutocomplete = -1;

            FocusGameWindowIfNeed();
        }

        private void FocusGameWindowIfNeed()
        {
            if (Application.isPlaying && _recipient == 0)
            {
                EditorApplication.delayCall += () =>
                {
                    EditorApplication.ExecuteMenuItem("Window/General/Game");
                };
            }
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

        protected void OnFocus()
        {
            EditorGUI.FocusTextInControl("MessageTextField");

            _refocusNextFrame = true;
            Repaint();
        }
    }
}

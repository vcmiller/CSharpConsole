using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

using UnityEditor;

using UnityEngine;

namespace CSharpConsole {
    public class CSharpConsoleWindow : EditorWindow {
        [Serializable]
        private struct LogEntry {
            public string _text;
            public LogEntryType _type;
        }

        private enum LogEntryType {
            Input, Output, Error,
        }
        
        [SerializeField] private string _command;
        [SerializeField] private List<LogEntry> _output = new List<LogEntry>();
        [SerializeField] private Vector2 _scrollPos;

        private ScriptState _scriptState = null;
        private GUIStyle _errorStyle;
        private GUIStyle _inputStyle;

        [MenuItem("Window/C# Console")]
        public static void Open() {
            GetWindow(typeof(CSharpConsoleWindow)).Show();
        }

        private void OnEnable() {
            _errorStyle = new GUIStyle(EditorStyles.label) {
                normal = {textColor = Color.red},
                wordWrap = true,
            };

            _inputStyle = new GUIStyle(EditorStyles.label) {
                normal = {textColor = Color.green},
                fontStyle = FontStyle.Italic,
            };
        }

        private void OnGUI() {
            EditorGUILayout.BeginHorizontal();
            _command = EditorGUILayout.TextField(_command);
            if (GUILayout.Button("Run", GUILayout.Width(100))) {
                _output.Add(new LogEntry {_text = _command, _type = LogEntryType.Input});
                try {
                    object result = RunCommand(_command);
                    if (result is IEnumerable array) {
                        _output.Add(new LogEntry{_text = result.ToString(), _type = LogEntryType.Output});
                        foreach (var element in array) {
                            _output.Add(new LogEntry{_text = $"\t{element?.ToString() ?? "null"}", _type = LogEntryType.Output});
                        }
                    } else {
                        _output.Add(new LogEntry{_text = result?.ToString() ?? "null", _type = LogEntryType.Output});
                    }
                } catch (Exception ex) {
                    _output.Add(new LogEntry{_text = ex.ToString(), _type = LogEntryType.Error});
                }
            }

            if (GUILayout.Button("Clear", GUILayout.Width(100))) {
                _output = new List<LogEntry>();
            }

            if (GUILayout.Button("Reset", GUILayout.Width(100))) {
                _scriptState = null;
                _output = new List<LogEntry>();
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));
            EditorGUI.BeginDisabledGroup(true);
            foreach (LogEntry entry in _output) {
                if (entry._type == LogEntryType.Error) {
                    EditorGUILayout.LabelField(entry._text, _errorStyle);
                } else if (entry._type == LogEntryType.Input) {
                    EditorGUILayout.LabelField(entry._text, _inputStyle);
                } else {
                    EditorGUILayout.LabelField(entry._text);
                }
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndScrollView();
        }

        private object RunCommand(string command) {
            var asms = AppDomain.CurrentDomain.GetAssemblies(); // .SingleOrDefault(assembly => assembly.GetName().Name == "MyAssembly");
            var options = ScriptOptions.Default;
            foreach (Assembly asm in asms)
            {
                if (asm.IsDynamic || string.IsNullOrEmpty(asm.Location)) {
                    continue;
                }
                options = options.AddReferences(asm);
            }

            options = options.AddImports("UnityEngine");

            if (_scriptState == null) {
                _scriptState = CSharpScript.RunAsync(command, options).Result;
            } else {
                _scriptState = _scriptState.ContinueWithAsync(command, options).Result;
            }

            return _scriptState.ReturnValue;
        }
    }
}
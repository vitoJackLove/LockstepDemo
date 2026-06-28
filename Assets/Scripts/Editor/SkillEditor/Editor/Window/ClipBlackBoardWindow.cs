using System;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace GameContent
{
    /// <summary>
    /// 黑板面板
    /// </summary>
    public class ClipBlackBoardWindow : EditorWindow
    {
        /// <summary>
        /// 选中的节点
        /// </summary>
        private static TaskClip selectClip;

        private static BlackBoardVariable blackBoardVariable;

        private static SkillTimelineEditorWindow skillTimelineWindow;

        /// <summary>
        /// 输入框的高度
        /// </summary>
        private int _inputFieldHight = 20;

        private static ClipBlackBoardWindow boardWindow;

        public static void ShowWindow()
        {
            if (boardWindow != null)
            {
                boardWindow.Close();
            }
        }

        public static void SetSelectClip(TaskClip taskClip, BlackBoardVariable blackBoard,
            SkillTimelineEditorWindow window = null)
        {
            selectClip = taskClip;
            blackBoardVariable = blackBoard;
            skillTimelineWindow = window;
        }

        [Obsolete("Obsolete")]
        private void DrawGui()
        {
            if (selectClip == null)
            {
                return;
            }

            GUIStyle dropdownTitleStyle = new GUIStyle()
            {
                name = "datasource-dropdown-title",
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                fixedHeight = 26,
                border = new RectOffset(1, 1, 1, 1),
                normal = new GUIStyleState()
                {
                    textColor = Color.cyan
                }
            };

            Type t = selectClip.GetType();
            
            FieldInfo[] memberInfos = t.GetFields();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"{t.GetCustomAttribute<ClipNameAttribute>().ClipName}", dropdownTitleStyle);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical();

            for (int i = 0; i < memberInfos.Length; i++)
            {
                FieldInfo propertyInfo = memberInfos[i];

                Type propertyType = propertyInfo.FieldType;

                ShowIfAttribute showIf = propertyInfo.GetCustomAttribute<ShowIfAttribute>();

                bool isShow =  CheckShow(showIf,memberInfos);

                if (!isShow)
                {
                    continue;
                }
                
                string propertyName;

                EditorVariableAttribute editorVariableAttribute =
                    propertyInfo.GetCustomAttribute<EditorVariableAttribute>();

                if (editorVariableAttribute != null)
                {
                    GUIStyle helpMessage = new GUIStyle()
                    {
                        name = "datasource-dropdown-title",
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = 10,
                        fontStyle = FontStyle.Bold,
                        fixedHeight = 20,
                        normal = new GUIStyleState()
                        {
                            textColor = Color.red
                        }
                    };

                    EditorGUILayout.LabelField("Editor使用的变量", helpMessage, GUILayout.Width(100),
                        GUILayout.Height(_inputFieldHight),
                        GUILayout.ExpandWidth(true));

                    propertyName = editorVariableAttribute.VariableName;
                }
                else
                {
                    propertyName = propertyInfo.GetCustomAttribute<VariableNameAttribute>()?.VariableName ??
                                   propertyInfo.Name;
                }

                object propertyValue = propertyInfo.GetValue(selectClip);

                if (propertyType.BaseType == typeof(BaseClipVariable))
                {
                    BaseClipVariable baseClipVariable = (BaseClipVariable)propertyValue;

                    DrawVariable(baseClipVariable.SVariableType(), propertyName, baseClipVariable.GetVariableValue(),
                        propertyInfo, true, baseClipVariable);
                }
                else
                {
                    DrawVariable(propertyType, propertyName, propertyValue, propertyInfo, false, null);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private bool CheckShow(ShowIfAttribute showIfAttribute,FieldInfo[] memberInfos)
        {
            if (showIfAttribute == null)
            {
                return true;
            }
            
            for (int i = 0; i < memberInfos.Length; i++)
            {
                if (string.Equals(showIfAttribute.Condition,  memberInfos[i].Name))
                {
                    if (memberInfos[i].FieldType == typeof(bool))
                    {
                        object value = memberInfos[i].GetValue(selectClip);

                        bool boolValue = (bool)value;

                        bool check = (bool)showIfAttribute.Value;

                        return check == boolValue;
                    }
                    else
                    {
                        return (int)showIfAttribute.Value == (int)memberInfos[i].GetValue(selectClip);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 画每个变量
        /// </summary>
        /// <param name="propertyType"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        /// <param name="propertyInfo"></param>
        /// <param name="isBlackVariable"></param>
        /// <param name="sBaseClipVariable"></param>
        [Obsolete("Obsolete")]
        private void DrawVariable(Type propertyType, string propertyName, object propertyValue, FieldInfo propertyInfo,
            bool isBlackVariable, BaseClipVariable sBaseClipVariable)
        {
            EditorGUILayout.BeginHorizontal();

            GUIStyle dropdownTitleStyle = GUI.skin.box;
            dropdownTitleStyle.fontSize = 12;
            dropdownTitleStyle.alignment = TextAnchor.LowerLeft;
            dropdownTitleStyle.fontStyle = FontStyle.Bold;

            GUIStyle variableStyle = GUI.skin.textField;
            variableStyle.fontSize = 12;
            variableStyle.alignment = TextAnchor.LowerLeft;

            EditorGUILayout.LabelField(propertyName, dropdownTitleStyle, GUILayout.Width(100),
                GUILayout.Height(_inputFieldHight), GUILayout.ExpandWidth(true));

            if (isBlackVariable && sBaseClipVariable.isBindBlackBoardVariable)
            {
                if (string.IsNullOrEmpty(sBaseClipVariable.blackBoardVariableKey))
                {
                    DrawBlackBoardVariableDropdownButton(propertyType, sBaseClipVariable);
                }
                else
                {
                    EditorGUILayout.LabelField(sBaseClipVariable.blackBoardVariableKey, dropdownTitleStyle,
                        GUILayout.Height(_inputFieldHight), GUILayout.ExpandWidth(true));
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                object tempIntValue =
                    SkillVariableDrawHelp.DrawVariable(propertyType, propertyValue, _inputFieldHight, 200, true);

                if (EditorGUI.EndChangeCheck())
                {
                    if (isBlackVariable)
                    {
                        sBaseClipVariable.SetVariableValue(tempIntValue);
                    }
                    else
                    {
                        propertyInfo.SetValue(selectClip, tempIntValue);
                    }

                    SaveSelectedClip();
                }
            }

            if (isBlackVariable)
            {
                EditorGUI.BeginChangeCheck();
                bool bind = EditorGUILayout.Toggle(sBaseClipVariable.isBindBlackBoardVariable,
                    GUILayout.Height(_inputFieldHight), GUILayout.Width(_inputFieldHight));

                if (EditorGUI.EndChangeCheck())
                {
                    sBaseClipVariable.SetBindBlackBoardVariable(bind);
                    SaveSelectedClip();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制选择的黑板变量
        /// </summary>
        private void DrawBlackBoardVariableDropdownButton(Type type, BaseClipVariable sBaseClipVariable)
        {
            if (EditorGUILayout.DropdownButton(new GUIContent("选择变量"), FocusType.Keyboard))
            {
                GenericMenu genericMenu = new GenericMenu();

                for (int i = 0; i < blackBoardVariable.Variables.Count; i++)
                {
                    SkillBlackVariable skillBlackVariable = blackBoardVariable.Variables[i];

                    if (skillBlackVariable.VariableType() == type)
                    {
                        genericMenu.AddItem(new GUIContent($"{skillBlackVariable.variableKey}"), false,
                            () =>
                            {
                                sBaseClipVariable.SetBindBlackBoardVariableKey(skillBlackVariable);
                                SaveSelectedClip();
                            });
                    }
                }

                genericMenu.ShowAsContext();
            }
        }

        [Obsolete("Obsolete")]
        public void OnGUI()
        {
            ProcessKeyboardShortcuts();
            DrawGui();
        }

        private static void ProcessKeyboardShortcuts()
        {
            var currentEvent = UnityEngine.Event.current;
            if (currentEvent.type != UnityEngine.EventType.KeyDown || EditorGUIUtility.editingTextField)
            {
                return;
            }

            if (currentEvent.keyCode == KeyCode.Delete && skillTimelineWindow != null &&
                skillTimelineWindow.DeleteSelectedClip())
            {
                currentEvent.Use();
            }
        }

        private static void SaveSelectedClip()
        {
            if (selectClip == null)
            {
                return;
            }

            EditorUtility.SetDirty(selectClip);
            skillTimelineWindow?.SaveTimelineAssetIfAutoSave();
        }
    }
}

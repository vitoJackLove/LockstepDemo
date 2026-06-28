using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 变量类型面板
/// </summary>
public class VariableTypeWindow : EditorWindow
{
    private static SkillTimelineEditorWindow blackBoardWindow;

    private static List<SkillBlackVariable> _blackVariables = new List<SkillBlackVariable>();

    public static void ShowWindow(SkillTimelineEditorWindow boardWindow)
    {
        blackBoardWindow = boardWindow;

        _blackVariables = new List<SkillBlackVariable>();

        for (int i = 0; i < SkillVariableType.SkillTimeLineVariableType.Length; i++)
        {
            Type type = SkillVariableType.SkillTimeLineVariableType[i];

            SkillBlackVariable blackVariable = SkillVariableDrawHelp.GetVariableInitValue(type);

            _blackVariables.Add(blackVariable);
        }

        GetWindow<VariableTypeWindow>("变量类型面板");
    }

    public void OnGUI()
    {
        if (_blackVariables == null)
        {
            return;
        }

        EditorGUILayout.BeginVertical();

        for (int i = 0; i < _blackVariables.Count; i++)
        {
            SkillBlackVariable variable = _blackVariables[i];

            EditorGUILayout.BeginHorizontal();

            variable.variableKey = EditorGUILayout.TextField(variable.variableKey);

            if (GUILayout.Button($"{variable.VariableType().Name}"))
            {
                if (string.IsNullOrEmpty(variable.variableKey))
                {
                    if (SkillEditorBlackBoard.CheckKeyInvalid(variable.variableKey))
                    {
                        blackBoardWindow.AddVariable(variable);

                        this.Close();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("错误!", "重复的变量名字...", "确认");
                    }
                }
                else
                {
                        EditorUtility.DisplayDialog("错误!", "变量名字不能为空...", "确认");
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    public void OnDestroy()
    {
        blackBoardWindow = null;
        _blackVariables.Clear();
    }
}

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 黑板
/// </summary>
public static class SkillEditorBlackBoard
{
    private static int variableHight = 20;

    /// <summary>
    /// 变量Key
    /// </summary>
    private static List<string> variableKeyList = new List<string>();

    public static void InitBlackKey(BlackBoardVariable blackBoardWindow)
    {
        if (blackBoardWindow == null)
        {
            return;
        }

        RefreshVariable(blackBoardWindow);
    }

    /// <summary>
    /// 检测key 是否无效
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static bool CheckKeyInvalid(string key)
    {
        if (variableKeyList.Contains(key))
        {
            return false;
        }

        return true;
    }

    public static void RefreshVariable(BlackBoardVariable blackBoardWindow)
    {
        variableKeyList.Clear();

        for (int i = 0; i < blackBoardWindow.Variables.Count; i++)
        {
            variableKeyList.Add(blackBoardWindow.Variables[i].variableKey);
        }
    }

    [Obsolete("Obsolete")]
    public static void DrawBlackBoardWindow(float variableWidth, float blackBoardWidth, SkillLineAsset asset,
        SkillTimelineEditorWindow window)
    {
        if (asset == null)
        {
            return;
        }

        if (asset.blackBoardVariable == null)
        {
            asset.blackBoardVariable = new BlackBoardVariable();
        }

        Texture2D texture =
            EditorGUIUtility.Load($"Assets/GameMain/Scripts/Editor/SkillEditor/Icons/Halt.png") as Texture2D;

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
                textColor = Color.yellow
            }
        };

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("变量KEY", dropdownTitleStyle, GUILayout.Width(variableWidth), GUILayout.Height(12));

        EditorGUILayout.LabelField("变量VALUE", dropdownTitleStyle, GUILayout.Width(variableWidth),
            GUILayout.Height(12));

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical();

        for (int i = 0; i < asset.blackBoardVariable.Variables.Count; i++)
        {
            SkillBlackVariable elementData = asset.blackBoardVariable.Variables[i];

            if (elementData == null)
            {
                continue;
            }

            EditorGUILayout.BeginHorizontal();

            GUIStyle style = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState() { textColor = Color.white ,background = Texture2D.linearGrayTexture}
            };
            
            //绘制变量的key
            EditorGUILayout.LabelField(elementData.variableKey, style, GUILayout.Height(variableHight),
                GUILayout.Width(variableWidth));

            elementData.SetValue(SkillVariableDrawHelp.DrawVariable(elementData.VariableType(),
                elementData.GetValue, variableHight, variableWidth, false));

            //移除变量
            if (GUILayout.Button(texture, GUILayout.Width(20), GUILayout.Height(20)))
            {
                window.RemoveVariable(i, elementData);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        EditorGUILayout.EndVertical();

        if (GUILayout.Button("添加一个变量", GUILayout.Height(20), GUILayout.Width(300)))
        {
            VariableTypeWindow.ShowWindow(window);
        }
    }
}
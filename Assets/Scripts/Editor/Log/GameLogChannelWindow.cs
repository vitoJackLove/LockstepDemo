using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class GameLogChannelWindow : EditorWindow
{
    [MenuItem("Tools/Log Channels")]
    public static void OpenWindow()
    {
        GetWindow<GameLogChannelWindow>("Log Channels");
    }

    private void OnEnable()
    {
        GameLog.LoadEditorPrefs();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Game Log Channels", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        foreach (GameLogChannel channel in Enum.GetValues(typeof(GameLogChannel)))
        {
            bool enabled = GameLog.IsChannelEnabled(channel);
            bool nextEnabled = EditorGUILayout.ToggleLeft(channel.ToString(), enabled);
            if (nextEnabled != enabled)
            {
                SetChannelEnabled(channel, nextEnabled, true);
            }
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Enable All"))
        {
            SetAllChannels(true);
        }

        if (GUILayout.Button("Disable Noisy"))
        {
            DisableNoisyChannels();
        }

        if (GUILayout.Button("Reset Defaults"))
        {
            ResetDefaults();
        }

        if (GUILayout.Button("Clear Console"))
        {
            ClearConsole();
        }

        EditorGUILayout.EndHorizontal();
    }

    private static void SetAllChannels(bool enabled)
    {
        foreach (GameLogChannel channel in Enum.GetValues(typeof(GameLogChannel)))
        {
            SetChannelEnabled(channel, enabled, true);
        }
    }

    private static void DisableNoisyChannels()
    {
        SetAllChannels(true);
        SetChannelEnabled(GameLogChannel.Rollback, false, true);
        SetChannelEnabled(GameLogChannel.AgentTest, false, true);
        SetChannelEnabled(GameLogChannel.EditorTool, false, true);
    }

    private static void ResetDefaults()
    {
        foreach (GameLogChannel channel in Enum.GetValues(typeof(GameLogChannel)))
        {
            EditorPrefs.DeleteKey(GetPrefsKey(channel));
        }

        GameLog.ResetDefaults();
    }

    private static void SetChannelEnabled(GameLogChannel channel, bool enabled, bool save)
    {
        GameLog.SetChannelEnabled(channel, enabled);

        if (save)
        {
            EditorPrefs.SetBool(GetPrefsKey(channel), enabled);
        }
    }

    private static string GetPrefsKey(GameLogChannel channel)
    {
        return GameLog.GetEditorPrefsKey(channel);
    }

    private static void ClearConsole()
    {
        Assembly editorAssembly = Assembly.GetAssembly(typeof(Editor));
        Type logEntries = editorAssembly.GetType("UnityEditor.LogEntries");
        MethodInfo clear = logEntries?.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
        clear?.Invoke(null, null);
    }
}

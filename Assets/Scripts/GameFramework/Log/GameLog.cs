using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class GameLog
{
#if UNITY_EDITOR
    private const string EditorPrefsPrefix = "RoguelikeMaster.GameLog.Channel.";
#endif

    private enum GameLogLevel
    {
        Debug,
        Info,
        Warn,
        Error
    }

    private static readonly Dictionary<GameLogChannel, bool> ChannelStates = new Dictionary<GameLogChannel, bool>();

    static GameLog()
    {
        ResetDefaults();
#if UNITY_EDITOR
        LoadEditorPrefs();
#endif
    }

    public static void Debug(GameLogChannel channel, string message)
    {
        Write(channel, GameLogLevel.Debug, message);
    }

    public static void Info(GameLogChannel channel, string message)
    {
        Write(channel, GameLogLevel.Info, message);
    }

    public static void Warn(GameLogChannel channel, string message)
    {
        Write(channel, GameLogLevel.Warn, message);
    }

    public static void Error(GameLogChannel channel, string message)
    {
        Write(channel, GameLogLevel.Error, message);
    }

    public static void SetChannelEnabled(GameLogChannel channel, bool enabled)
    {
        ChannelStates[channel] = enabled;
    }

    public static bool IsChannelEnabled(GameLogChannel channel)
    {
        return !ChannelStates.TryGetValue(channel, out bool enabled) || enabled;
    }

    public static void ResetDefaults()
    {
        foreach (GameLogChannel channel in System.Enum.GetValues(typeof(GameLogChannel)))
        {
            ChannelStates[channel] = true;
        }
    }

#if UNITY_EDITOR
    public static void LoadEditorPrefs()
    {
        foreach (GameLogChannel channel in System.Enum.GetValues(typeof(GameLogChannel)))
        {
            ChannelStates[channel] = EditorPrefs.GetBool(GetEditorPrefsKey(channel), true);
        }
    }

    public static string GetEditorPrefsKey(GameLogChannel channel)
    {
        return EditorPrefsPrefix + channel;
    }
#endif

    private static void Write(GameLogChannel channel, GameLogLevel level, string message)
    {
        if (!ShouldWrite(channel, level))
        {
            return;
        }

        string formattedMessage = $"[{channel}][{level}] {message}";

        switch (level)
        {
            case GameLogLevel.Warn:
                UnityEngine.Debug.LogWarning(formattedMessage);
                break;
            case GameLogLevel.Error:
                UnityEngine.Debug.LogError(formattedMessage);
                break;
            default:
                UnityEngine.Debug.Log(formattedMessage);
                break;
        }
    }

    private static bool ShouldWrite(GameLogChannel channel, GameLogLevel level)
    {
        if (!IsChannelEnabled(channel))
        {
            return false;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        return true;
#else
        return level == GameLogLevel.Warn;
#endif
    }
}

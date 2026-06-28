using UnityEditor;
using UnityEngine;

public static class Controller
{
    private static ControllerEvent controllerEvent;

    static Controller()
    {
        controllerEvent = new ControllerEvent();
    }

    public static void UpdateGUI(SkillTimelineEditorWindow window, Rect rect)
    {
        rect = new Rect(rect.x + 10f, rect.y, rect.width - 20f, rect.height);
        float topHeight = rect.height / 3f;
        float saveHeight = rect.height / 3f;
        float bottomHeight = rect.height - topHeight - saveHeight;
        
        //绘制帧率
        float fpsWidth = rect.width / 4;
        
        var fpsRect = new Rect(rect.x, rect.y, fpsWidth, topHeight);
        
        if (GUI.Button(fpsRect, window.FPS + "帧", EditorStyles.popup))
        {
            GenericMenu menu = new GenericMenu();
            // 添加菜单项
            //拿到所有StandardTrack的子类型
            menu.AddItem(new GUIContent("30"), window.FPS == 30, () => window.FPS = 30);
            menu.AddItem(new GUIContent("60"), window.FPS == 60, () => window.FPS = 60);
            menu.AddItem(new GUIContent("120"), window.FPS == 120, () => window.FPS = 120);
            // 显示菜单
            menu.ShowAsContext();
        }

        float rectX = rect.x + fpsWidth;

        //绘制ObjectFIled
        float contextObjectWidth = rect.width / 4;
        var contextObject = new Rect(rectX, rect.y, contextObjectWidth, topHeight);
        
        //绘制一个灰色的box lable为 点击选择文件
        {
            window.objectContext =
                EditorGUI.ObjectField(contextObject, window.objectContext, typeof(GameObject), true) as GameObject;
        }
        rectX += contextObjectWidth;

        //TODO: 序列化形式
        
        var objectFiledRect = new Rect(rectX, rect.y, rect.width - fpsWidth - contextObjectWidth, topHeight);
        {
            var tempObj = EditorGUI.ObjectField(objectFiledRect, window.Asset, typeof(SkillLineAsset), false);
            if (tempObj != window.Asset)
            {
                window.AssetPath = AssetDatabase.GetAssetPath(tempObj);
            }
        }

        DrawSaveControls(window, new Rect(rect.x, rect.y + topHeight, rect.width, saveHeight));

        float itemWidth = rect.width / 5;
        
        float x = 0;
        
        for (int i = 0; i < 5; i++)
        {
            var itemRect = new Rect(rect.x + x, rect.y + topHeight + saveHeight, itemWidth,
                bottomHeight);
            GUI.backgroundColor = new Color(200 / 255f, 200 / 255f, 200 / 255f, 1f);
            switch (i)
            {
                case 0:
                    ToMostBegin(window, itemRect);
                    break;
                case 1:
                    ToPreFrame(window, itemRect);
                    break;
                case 2:
                    Play(window, itemRect);
                    break;
                case 3:
                    ToNextFrame(window, itemRect);
                    break;
                case 4:
                    ToMostEnd(window, itemRect);
                    break;
            }

            x += itemWidth;
        }
    }

    private static void DrawSaveControls(SkillTimelineEditorWindow window, Rect rect)
    {
        float autoSaveWidth = rect.width * 0.55f;
        var autoSaveRect = new Rect(rect.x, rect.y, autoSaveWidth, rect.height);
        var saveRect = new Rect(rect.x + autoSaveWidth + 4f, rect.y,
            rect.width - autoSaveWidth - 4f, rect.height);

        window.AutoSave = GUI.Toggle(autoSaveRect, window.AutoSave, "自动保存", "Button");

        using (new EditorGUI.DisabledScope(window.Asset == null))
        {
            if (GUI.Button(saveRect, "手动保存"))
            {
                window.SaveTimelineAsset();
            }
        }
    }

    private static void ToMostBegin(SkillTimelineEditorWindow window, Rect rect)
    {
        GUIContent gotoBeginingContent =
            L10n.IconContent("Animation.FirstKey", "Go to the beginning of the timeline");
        if (GUI.Button(rect, gotoBeginingContent))
        {
            controllerEvent.ControllerType = ControllerType.ToMostBegin;
            EventCenter.TrigerEvent(window, controllerEvent);
        }
    }

    private static void ToPreFrame(SkillTimelineEditorWindow window, Rect rect)
    {
        GUIContent previousFrameContent = L10n.IconContent("Animation.PrevKey", "Go to the previous frame");
        if (GUI.Button(rect, previousFrameContent))
        {
            controllerEvent.ControllerType = ControllerType.ToPre;
            EventCenter.TrigerEvent(window, controllerEvent);
        }
    }

    private static void Play(SkillTimelineEditorWindow window, Rect rect)
    {
        GUIContent playContent = L10n.IconContent("Animation.Play", "Play the timeline (Space)");
        if (GUI.Button(rect, playContent, "Button"))
        {
            controllerEvent.ControllerType = ControllerType.Play;
            EventCenter.TrigerEvent(window, controllerEvent);
        }
    }

    private static void ToNextFrame(SkillTimelineEditorWindow window, Rect rect)
    {
        GUIContent nextFrameContent = L10n.IconContent("Animation.NextKey", "Go to the next frame");
        if (GUI.Button(rect, nextFrameContent))
        {
            controllerEvent.ControllerType = ControllerType.ToNext;
            EventCenter.TrigerEvent(window, controllerEvent);
        }
    }

    private static void ToMostEnd(SkillTimelineEditorWindow window, Rect rect)
    {
        GUIContent gotoEndContent =
            L10n.IconContent("Animation.LastKey", "Go to the end of the timeline");
        if (GUI.Button(rect, gotoEndContent))
        {
            controllerEvent.ControllerType = ControllerType.ToMostEnd;
            EventCenter.TrigerEvent(window, controllerEvent);
        }
    }
}

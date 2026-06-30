using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 浏览英雄/怪物与其关联 SkillTimeLine，并一键跳转技能编辑器。
/// </summary>
public class EntitySkillTimelineBrowserWindow : OdinMenuEditorWindow
{
    private const string WindowTitle = "实体技能 TimeLine 浏览器";

    [MenuItem("Tools/技能/实体技能TimeLine浏览器")]
    public static void OpenWindow()
    {
        EntitySkillTimelineBrowserWindow window = GetWindow<EntitySkillTimelineBrowserWindow>();
        window.titleContent = new GUIContent(WindowTitle);
        window.minSize = new Vector2(980, 620);
        window.Show();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        titleContent = new GUIContent(WindowTitle);
    }

    protected override OdinMenuTree BuildMenuTree()
    {
        OdinMenuTree tree = new OdinMenuTree(true)
        {
            Config =
            {
                DrawSearchToolbar = true,
                DefaultMenuStyle = OdinMenuStyle.TreeViewStyle,
            },
        };

        foreach (EntitySkillTimelineViewModel hero in EntitySkillTimelineBrowserData.BuildHeroViewModels())
        {
            string menuPath = $"英雄/{hero.EntityName} ({hero.EntityId})";
            tree.Add(menuPath, hero);
        }

        foreach (EntitySkillTimelineViewModel monster in EntitySkillTimelineBrowserData.BuildMonsterViewModels())
        {
            string menuPath = $"怪物/{monster.EntityName} ({monster.EntityId})";
            tree.Add(menuPath, monster);
        }

        tree.EnumerateTree().AddIcons<EntitySkillTimelineViewModel>(x => x.Icon);
        return tree;
    }

    protected override void OnBeginDrawEditors()
    {
        OdinMenuTreeSelection selected = MenuTree.Selection;

        Sirenix.Utilities.Editor.SirenixEditorGUI.BeginHorizontalToolbar();
        if (Sirenix.Utilities.Editor.SirenixEditorGUI.ToolbarButton("刷新"))
        {
            ForceMenuTreeRebuild();
        }

        if (Sirenix.Utilities.Editor.SirenixEditorGUI.ToolbarButton("打开技能编辑器"))
        {
            SkillTimelineEditorWindow.OpenWindow();
        }

        if (selected != null && selected.SelectedValue is EntitySkillTimelineViewModel viewModel)
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{viewModel.EntityName} · {viewModel.SkillCount} 个 SkillTimeLine");
        }

        Sirenix.Utilities.Editor.SirenixEditorGUI.EndHorizontalToolbar();
    }
}

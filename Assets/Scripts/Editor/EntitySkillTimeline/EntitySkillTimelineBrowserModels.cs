using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

public enum EntitySkillTimelineKind
{
    Hero,
}

/// <summary>
/// 单个实体关联的技能 TimeLine 浏览数据。
/// </summary>
[Serializable]
public class EntitySkillTimelineViewModel
{
    [HorizontalGroup("Header", 64)]
    [PreviewField(64, ObjectFieldAlignment.Left)]
    [HideLabel]
    [ReadOnly]
    public Sprite Icon;

    [VerticalGroup("Header/Info")]
    [ReadOnly]
    [LabelText("名称")]
    public string EntityName;

    [VerticalGroup("Header/Info")]
    [ReadOnly]
    [LabelText("ID")]
    public int EntityId;

    [VerticalGroup("Header/Info")]
    [ReadOnly]
    [LabelText("类型")]
    public EntitySkillTimelineKind Kind;

    [VerticalGroup("Header/Info")]
    [ReadOnly]
    [LabelText("技能数量")]
    public int SkillCount;

    [Title("关联 SkillTimeLine")]
    [TableList(IsReadOnly = true, ShowIndexLabels = false, AlwaysExpanded = true, DrawScrollView = true)]
    public List<SkillTimelineBindingRow> Skills = new List<SkillTimelineBindingRow>();

    [ShowInInspector]
    [ReadOnly]
    [LabelText("缺失资产")]
    [ShowIf(nameof(HasMissingAssets))]
    [GUIColor(1f, 0.65f, 0.65f)]
    private string MissingAssetSummary => $"{MissingAssetCount} 个 SkillTimeLine 资产未找到";

    public int MissingAssetCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < Skills.Count; i++)
            {
                if (!Skills[i].AssetExists)
                {
                    count++;
                }
            }

            return count;
        }
    }

    private bool HasMissingAssets => MissingAssetCount > 0;
}

/// <summary>
/// 一行技能绑定，包含预览信息与跳转按钮。
/// </summary>
[Serializable]
public class SkillTimelineBindingRow
{
    [TableColumnWidth(60, Resizable = false)]
    [ReadOnly]
    [LabelText("技能ID")]
    public int SkillId;

    [TableColumnWidth(88, Resizable = false)]
    [ReadOnly]
    [LabelText("绑定")]
    public string BindingLabel;

    [TableColumnWidth(90, Resizable = false)]
    [ReadOnly]
    [ShowIf(nameof(ShowCommandType))]
    [LabelText("指令")]
    public CommandType CommandType;

    [TableColumnWidth(220)]
    [ReadOnly]
    [LabelText("配置路径")]
    public string ConfigPath;

    [TableColumnWidth(56, Resizable = false)]
    [ReadOnly]
    [LabelText("FPS")]
    public int Fps;

    [TableColumnWidth(56, Resizable = false)]
    [ReadOnly]
    [LabelText("时长")]
    public int Duration;

    [TableColumnWidth(56, Resizable = false)]
    [ReadOnly]
    [LabelText("轨道")]
    public int TrackCount;

    [TableColumnWidth(72, Resizable = false)]
    [ReadOnly]
    [GUIColor(nameof(GetAssetStatusColor))]
    [LabelText("状态")]
    public string AssetStatus;

    [HideInInspector]
    public bool AssetExists;

    private bool ShowCommandType => CommandType != default;

    private Color GetAssetStatusColor => AssetExists ? Color.green : Color.red;

    [Button("编辑")]
    [TableColumnWidth(56, Resizable = false)]
    private void OpenSkillEditor()
    {
        SkillTimelineEditorWindow.OpenWindowWithConfigPath(ConfigPath);
    }

    [Button("定位")]
    [TableColumnWidth(56, Resizable = false)]
    private void PingAsset()
    {
        SkillTimelinePathUtility.PingSkillAsset(ConfigPath);
    }

    public static SkillTimelineBindingRow FromHeroSkill(HeroSkillConfig config, string bindingLabel, string configPath)
    {
        SkillTimelineBindingRow row = CreateBaseRow(config.skillId, bindingLabel, configPath);
        row.CommandType = config.keyCode;
        return row;
    }

    private static SkillTimelineBindingRow CreateBaseRow(int skillId, string bindingLabel, string configPath)
    {
        SkillTimelineBindingRow row = new SkillTimelineBindingRow
        {
            SkillId = skillId,
            BindingLabel = bindingLabel,
            ConfigPath = configPath ?? string.Empty,
        };

        if (SkillTimelinePathUtility.TryResolveAsset(configPath, out SkillLineAsset asset, out _))
        {
            row.AssetExists = true;
            row.AssetStatus = "已找到";
            row.Fps = asset.fps;
            row.Duration = asset.duration;
            row.TrackCount = asset.tracks?.Count ?? 0;
        }
        else
        {
            row.AssetExists = false;
            row.AssetStatus = string.IsNullOrWhiteSpace(configPath) ? "未配置" : "缺失";
        }

        return row;
    }
}

/// <summary>
/// 从 HeroAssets 构建浏览数据。
/// </summary>
public static class EntitySkillTimelineBrowserData
{
    private const string HeroAssetsPath = "Assets/GameAssetConfig/HeroAssets.asset";

    public static List<EntitySkillTimelineViewModel> BuildHeroViewModels()
    {
        List<EntitySkillTimelineViewModel> result = new List<EntitySkillTimelineViewModel>();
        HeroAssets heroAssets = AssetDatabase.LoadAssetAtPath<HeroAssets>(HeroAssetsPath);
        if (heroAssets == null)
        {
            Debug.LogWarning($"未找到英雄配置: {HeroAssetsPath}");
            return result;
        }

        for (int i = 0; i < heroAssets.HeroAssetsConfigList.Count; i++)
        {
            HeroAssetsConfig config = heroAssets.HeroAssetsConfigList[i];
            if (config == null)
            {
                continue;
            }

            EntitySkillTimelineViewModel viewModel = new EntitySkillTimelineViewModel
            {
                EntityId = config.assetsId,
                EntityName = string.IsNullOrWhiteSpace(config.heroName) ? $"Hero_{config.assetsId}" : config.heroName,
                Kind = EntitySkillTimelineKind.Hero,
                Icon = config.heroIcon,
            };

            AppendHeroSkillBindings(viewModel, config);
            viewModel.SkillCount = viewModel.Skills.Count;
            result.Add(viewModel);
        }

        result.Sort(CompareViewModels);
        return result;
    }

    private static void AppendHeroSkillBindings(EntitySkillTimelineViewModel viewModel, HeroAssetsConfig config)
    {
        for (int i = 0; i < config.initSkillList.Count; i++)
        {
            HeroSkillConfig skillConfig = config.initSkillList[i];
            if (skillConfig == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(skillConfig.skillDownAssetsPath))
            {
                viewModel.Skills.Add(SkillTimelineBindingRow.FromHeroSkill(
                    skillConfig,
                    GetHeroBindingLabel(skillConfig, true),
                    skillConfig.skillDownAssetsPath));
            }

            if (!string.IsNullOrWhiteSpace(skillConfig.skillUpAssetsPath))
            {
                viewModel.Skills.Add(SkillTimelineBindingRow.FromHeroSkill(
                    skillConfig,
                    GetHeroBindingLabel(skillConfig, false),
                    skillConfig.skillUpAssetsPath));
            }
        }
    }

    private static string GetHeroBindingLabel(HeroSkillConfig skillConfig, bool isDownPath)
    {
        if (skillConfig.commandState == WorldContent.CommandExecuteState.DownUp)
        {
            return isDownPath ? "按下" : "抬起";
        }

        if (skillConfig.commandState == WorldContent.CommandExecuteState.OnlyUp)
        {
            return "抬起";
        }

        return "按下";
    }

    private static int CompareViewModels(EntitySkillTimelineViewModel left, EntitySkillTimelineViewModel right)
    {
        return left.EntityId.CompareTo(right.EntityId);
    }
}

# UI 模块

> 📍 位置: `Assets/Scripts/RunTime/UI/`
> 🔗 父文档: [RunTime CLAUDE.md](../CLAUDE.md)
> 📊 完善度: 🔴 基础

## 模块职责

UI 模块负责游戏界面的管理和交互，包括：
- **UI 面板**: 窗口/面板的加载和显示
- **数据绑定**: UI 与数据模型的绑定
- **动画效果**: UI 动画和过渡效果

## 目录结构

```
UI/
├── Content.cs                   # UI 内容基类
├── BindData/                   # 数据绑定
│   └── BindDataKey.cs         # 绑定数据键
├── UIEffect/                   # UI 特效
│   └── UIEffect.cs            # UI 效果实现
├── Model/                       # UI 模型
│   └── ProgressBarModel.cs   # 进度条模型
└── ...
```

## 核心概念

### Content 基类

```csharp
public class Content
{
    // 显示面板
    public virtual void OnShow() { }

    // 隐藏面板
    public virtual void OnHide() { }

    // 刷新数据
    public virtual void OnRefresh() { }
}
```

## 使用示例

```csharp
// 打开 UI
GameEntry.UI.OpenUIForm<HeroPanel>();

// 刷新 UI
heroPanel.OnRefresh();

// 关闭 UI
GameEntry.UI.CloseUIForm("HeroPanel");
```

## 数据绑定

```csharp
// 绑定数据到 UI
BindDataKey.SetValue<int>(panel, "Health", 100);
```

## 注意事项

- **层级管理**: UI 面板有层级顺序
- **资源释放**: 关闭 UI 时注意资源释放
- **事件解绑**: 页面关闭时解绑所有事件

---

<!-- CLAUDE_MD_META
version: 1.0
last_updated: 2026-03-05
completeness: basic
-->

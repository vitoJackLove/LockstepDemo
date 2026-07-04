# Task 3 Report: 拆分 AddressablesSyncStep 与 BuildLayoutGuard

**Date:** 2026-07-04  
**Status:** COMPLETE

## Summary

将 `RuntimeAddressablesConfigurator` 中的 Addressables 同步逻辑与 Build Layout Guard 拆分到热更 Pipeline 模块，`RuntimeAddressablesConfigurator` 仅保留 `SyncRuntimeAssets()` 转发壳。

## Changes

| File | Action |
|------|--------|
| `Assets/Scripts/Editor/HotUpdate/Internal/AddressablesBuildLayoutGuard.cs` | Created — `[InitializeOnLoad]` guard，`Rogue.Editor.HotUpdate.Internal` 命名空间，已移除 MenuItem |
| `Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/AddressablesSyncStep.cs` | Created — `Execute(context)` + `ExecuteSyncOnly()`，含全部 GroupDefinitions / 扫描 / 同步逻辑 |
| `Assets/Scripts/Editor/Addressables/RuntimeAddressablesConfigurator.cs` | Slimmed — 仅 `[Obsolete] SyncRuntimeAssets()` → `AddressablesSyncStep.ExecuteSyncOnly()` |

## Interfaces Delivered

- `AddressablesSyncStep.Execute(HotUpdateBuildContext context)`
- `AddressablesSyncStep.ExecuteSyncOnly()` — public，供 BulletFactory 等遗留调用
- `AddressablesBuildLayoutGuard.PrepareForAddressablesBuild(bool logResult)`
- `RuntimeAddressablesConfigurator.SyncRuntimeAssets()` — 转发保留

## MenuItem Cleanup

已从 `RuntimeAddressablesConfigurator` 移除：
- `Tools/Addressables/同步运行时资源`
- `Tools/Addressables/构建本地内容`
- `Tools/Addressables/构建运行时内容`
- `Tools/Addressables/构建远程更新`

已从 `AddressablesBuildLayoutGuard` 移除：
- `Tools/Addressables/禁用构建布局报告`

## Verification

```
dotnet build Game.Editor.csproj -nologo -v:minimal
```

**Result:** Build succeeded — 0 errors, 45 warnings (pre-existing + expected CS0618 on `SyncRuntimeAssets()` callers)

**Callers still compile:**
- `BulletQuickCreateWindow.cs`
- `MonsterQuickCreateWindow.cs`
- `HeroQuickCreateWindow.cs`
- `HybridCLRDllCopyTool.cs` (Task 4 will migrate)

## Commit

```
refactor(editor): extract AddressablesSyncStep from configurator
```

## Concerns

1. **`Game.Editor.csproj`** — 新文件需 Unity 重新生成 csproj 条目；本地验证时临时添加了 Compile Include，不应提交（auto-generated）。
2. **CS0618 warnings** — 内容工厂与 HybridCLRDllCopyTool 调用 `[Obsolete] SyncRuntimeAssets()` 产生预期警告；Task 4 迁移 HybridCLR 后可减少一处。
3. **BuildLayoutGuard 命名空间变更** — 后续 Task 5 的 `AddressablesFullBuildStep` / `AddressablesContentUpdateStep` 需引用 `Rogue.Editor.HotUpdate.Internal.AddressablesBuildLayoutGuard`（旧全局类已删除）。

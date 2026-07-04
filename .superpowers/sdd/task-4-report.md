# Task 4 Report: HybridClrBuildStep

## STATUS: PASS

## Summary

将 `HybridCLRDllCopyTool` 的 DLL 编译/拷贝/manifest 逻辑迁移至 Pipeline Step `HybridClrBuildStep`，删除旧 MenuItem 入口，构建后自动调用 `AddressablesSyncStep.Execute(context)`。

## Changes

| Action | File |
|--------|------|
| Create | `Assets/Scripts/Editor/HotUpdate/Pipeline/Steps/HybridClrBuildStep.cs` |
| Delete | `Assets/Scripts/Editor/HybridCLR/HybridCLRDllCopyTool.cs` |
| Delete | `Assets/Scripts/Editor/HybridCLR/HybridCLRDllCopyTool.cs.meta` |
| Update | `Game.Editor.csproj` — 替换 Compile 条目 |

## Interface

```csharp
namespace Rogue.Editor.HotUpdate.Pipeline.Steps
{
    public static class HybridClrBuildStep
    {
        public static void Execute(HotUpdateBuildContext context, bool compileBeforeCopy);
    }
}
```

### Behavior

1. `compileBeforeCopy == true` → `CompileDllCommand.CompileDll(context.BuildTarget, context.DevelopmentBuild)`
2. 拷贝热更 DLL 到 `Assets/HotUpdate/Code/Game.Runtime.dll.bytes`
3. 拷贝 AOT 元数据到 `Assets/HotUpdate/Code/AOT/*.dll.bytes`
4. 写入 `Assets/HotUpdate/Code/manifest.json`（资产侧 manifest，非 `Build/HotUpdateManifest.json`）
5. `AssetDatabase.Refresh()`
6. `AddressablesSyncStep.Execute(context)`

### Removed

- `Tools/HybridCLR/正式入口/*` MenuItem
- `EditorUtility.DisplayDialog` UI
- 对 `[Obsolete] RuntimeAddressablesConfigurator.SyncRuntimeAssets()` 的直接调用

## Grep Verification

```
rg HybridCLRDllCopyTool Assets/
→ 0 matches（仅 docs/.superpowers 历史文档保留提及）
```

## Build Verification

```
dotnet build Game.Editor.csproj -nologo -v:minimal
→ Exit code 0 (Build succeeded)
```

注：`Game.Editor.csproj` 需手动将 `HybridCLRDllCopyTool.cs` 条目替换为 `HybridClrBuildStep.cs`（Unity 重新生成 csproj 时会自动同步）。

## Manual Verification (Deferred)

Unity Editor 内调用 `HybridClrBuildStep.Execute(context, compileBeforeCopy: true)` 需在 Task 8/10 Pipeline/Window 完成后验收。预期：

- `Assets/HotUpdate/Code/Game.Runtime.dll.bytes` 更新
- `Assets/HotUpdate/Code/AOT/*.dll.bytes` 存在
- Console 无 Error

## Concerns

1. **无独立 MenuItem** — Phase 1 设计意图；在 `HotUpdatePublishWindow`（Task 10）完成前，Editor 内无 UI 入口直接触发 HybridCLR 同步。
2. **csproj 手改** — 若 Unity 重新生成 `Game.Editor.csproj` 会包含新文件；删除的旧文件引用也会自动清除。
3. **AOT 目录依赖** — 与旧工具相同，需先执行 HybridCLR Generate/AotDlls 或完成 il2cpp 打包，否则抛 `DirectoryNotFoundException`。

## Commit

```
refactor(editor): migrate HybridCLR copy logic to HybridClrBuildStep
```

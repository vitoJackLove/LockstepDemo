# Task 2 Report — HotUpdateManifest 读写与测试

## Contract

- **STATUS:** DONE_WITH_CONCERNS
- **Commits:** `7a04a7a`
- **Tests:** `dotnet build Game.Editor.csproj` + `Game.Editor.Tests.csproj` 均 0 errors；Unity Test Runner 尚未发现 `HotUpdateManifestTests`（需 Editor 域重载后手动验证）
- **Concerns:** 新增 `Game.Editor.Tests.asmdef` 与 `AssemblyInfo.cs`（`InternalsVisibleTo`）为测试编译所必需，超出 plan Step 5 的两文件提交列表；Unity MCP `run_tests` 在域重载前未枚举到新测试

---

## Deliverables

| File | Description |
|------|-------------|
| `Assets/Scripts/Editor/HotUpdate/Pipeline/HotUpdateManifest.cs` | `HotUpdateManifestData`、hash/patch 条目、`Load`/`Save`、`TryResolveContentStatePath`、`ComputeFileSha256` |
| `Assets/Scripts/Test/EditMode/HotUpdateManifestTests.cs` | Save/Load 往返与 manifest 优先解析 content state 测试 |
| `Assets/Scripts/Test/Game.Editor.Tests.asmdef` | 新建 EditMode 测试程序集，引用 `Game.Editor` |
| `Assets/Scripts/Editor/HotUpdate/AssemblyInfo.cs` | `InternalsVisibleTo("Game.Editor.Tests")` 暴露测试 hook |

**Namespace:** `Rogue.Editor.HotUpdate.Pipeline`

## Build Verification

```
dotnet build D:\UnityProject\lockstep\Game.Editor.csproj -nologo -v:minimal
dotnet build D:\UnityProject\lockstep\Game.Editor.Tests.csproj -nologo -v:minimal
```

Result: **Build succeeded** — 0 errors on both assemblies.

## Commit

```
feat(editor): add HotUpdateManifest read/write and tests
```

9 files changed, 235 insertions(+).

## Scope Notes

- 使用 `JsonUtility`（非 Newtonsoft）
- 公共类型已添加中文 XML 注释
- 未实现 Task 3+

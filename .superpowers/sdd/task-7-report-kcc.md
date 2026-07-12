# Task 7 Report — HeroAssets / MonsterAssets 配置字段

## Contract

- **STATUS:** DONE
- **Commits:** `7eed7964951c0bb4cf1163aedc6c2f028fccd4f5`
- **Base:** `c18f9cf7153d45a55a288a99eb85c4dbae78596a`
- **Tests:**
  - `dotnet build Assembly-CSharp.csproj -nologo -v:minimal` — 0 errors
- **Concerns:** 见下方

---

## Deliverables

| File | Description |
|------|-------------|
| `Assets/Scripts/RunTime/EntityAssetsConfig/HeroAssets.cs` | `HeroAssetsConfig` 新增 `movementMode` / `characterController` / `physicsBody` |
| `Assets/Scripts/RunTime/EntityAssetsConfig/MonsterAssets.cs` | `MonsterAssetsConfig` 新增相同三字段，默认 Rigidbody Kinematic + Box |
| `Assets/Scripts/Editor/HeroFactory/HeroQuickCreateWindow.cs` | 物理段 UI + 创建 config 时写入 CC 默认值 |
| `Assets/Scripts/Editor/MonsterFactory/MonsterQuickCreateWindow.cs` | 物理段 UI + 创建 config 时写入 Rigidbody 默认值 |

## Implementation Summary

### HeroAssetsConfig

- 在 `colliderDataList`（受击盒）**之前**插入三字段。
- 默认 `movementMode = CharacterController`。
- `characterController` / `physicsBody` 通过 Odin `ShowIf` 按范式切换显示。

### MonsterAssetsConfig

- 相同三字段布局。
- 默认 `movementMode = Rigidbody`。
- `physicsBody` 预置 Kinematic + 单 Box 碰撞体（`halfExtents = (0.5, 1, 0.5)`，`layer = Monster`）。

### QuickCreate 窗口

- **HeroQuickCreateWindow：** 新增 `DrawPhysicsSection`（受击盒之后），默认 CC 胶囊（r=0.5, h=2, center.y=1, layer=Hero）；`RegisterHeroConfig` 写入 `movementMode` / `characterController`。
- **MonsterQuickCreateWindow：** 新增 `DrawPhysicsSection`，默认 Rigidbody Kinematic + Box；`RegisterMonsterConfig` 写入 `movementMode` / `physicsBody`。

## Build Verification

```
dotnet build Assembly-CSharp.csproj -nologo -v:minimal
```

Result: **Build succeeded** — 0 errors，37 warnings（项目既有警告，与本次变更无关）。

## Commit

```
feat(kcc): add movementMode and physics settings to hero/monster configs
```

Hash: `7eed7964951c0bb4cf1163aedc6c2f028fccd4f5`

## Concerns

1. **现有 HeroAssets / MonsterAssets.asset 未迁移：** plan Task 12 负责批量迁移已有资产；本 Task 仅追加字段定义与 QuickCreate 默认值。
2. **Monster QuickCreate Rigidbody UI 为最小实现：** 仅编辑 bodyType 与首个 Box collider 的 halfExtents/layer；复合体完整编辑留待后续迭代。
3. **受击盒与物理体分离：** `colliderDataList` 保持独立，符合 plan 设计（命中判定 vs 移动碰撞）。

## Self-Review

1. **范围合规：** 配置字段 + QuickCreate 默认值均按 plan 880+ / Task 13–14 意图实现。
2. **字段顺序：** 物理字段位于 `colliderDataList` 之前。
3. **构建验证：** `Assembly-CSharp.csproj` 编译通过，无新增错误。

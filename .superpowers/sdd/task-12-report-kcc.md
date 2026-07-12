# Task 12 Report — GameAssetConfig 默认物理迁移

## Contract

- **STATUS:** DONE
- **Commits:** `12228f017a01e849d8dcf3a51a78e2ca505539b1`
- **Base:** `4a2cedab7b5b7f25cc42d84bd6764a4be6f768f0`
- **Tests:**
  - `dotnet build Assembly-CSharp.csproj -nologo -v:minimal` — 0 errors

---

## Deliverables

| File | Description |
|------|-------------|
| `Assets/GameAssetConfig/HeroAssets.asset` | 3 条 HeroAssetsConfig 写入 CC 默认物理 |
| `Assets/GameAssetConfig/MonsterAssets.asset` | 3 条 MonsterAssetsConfig 写入 Rigidbody Kinematic Box 默认物理 |

## Implementation Summary

### Step 1 — 英雄默认 CharacterController

对 `HeroAssetsConfigList` 全部 3 条目（1104、1205、1001）追加：

- `movementMode: 0`（CharacterController）
- `characterController`: radius=0.5, height=2, center=(0,1,0), layer=Hero (4)
- `physicsBody.colliders: []`（空列表）

### Step 2 — 怪物默认 Rigidbody Kinematic Box

对 `monsterAssetsConfigList` 全部 3 条目（2001、3101、4001）追加：

- `movementMode: 1`（Rigidbody）
- `physicsBody.bodyType: 1`（Kinematic）
- `physicsBody.colliders[0]`: Box, halfExtents=(0.5,1,0.5), layer=Monster (8)

### Step 3 — CubeEntity 地图块验证

只读确认，无代码变更：

- `CubeEntity.OnInit` 调用 `PhysicsEntityConfig.CreateDefaultStaticBox(this)` 注入 `PhysicsBodyData`
- `CreateDefaultStaticBox` 使用 `PhysicsBodyType.Static` + Box collider `layer=FPCollisionLayer.Wall`
- `MapSystem` 经 `EntitySystem.CreateEntity<CubeEntity>` 创建地图块，物理注册路径正确

## Build Verification

```
dotnet build Assembly-CSharp.csproj -nologo -v:minimal
```

Result: **Build succeeded** — 0 errors，37 warnings（项目既有警告）。

## Commit

```
chore(kcc): migrate hero/monster assets to new physics config
```

Hash: `12228f017a01e849d8dcf3a51a78e2ca505539b1`

## Concerns

1. **YAML 直编资产：** 计划允许 CLI 编辑 Unity 序列化 YAML；若 Unity Editor 重新 Save 可能重排字段顺序，不影响语义。
2. **Hero CC center=(0,1,0)：** 与 plan 一致；受击盒 colliderDataList 仍保留既有 offset，与 CC 胶囊独立。
3. **Monster CC 字段保留默认值：** Rigidbody 范式下 `characterController` 不参与注册，仅满足序列化完整性。

## Self-Review

1. **范围合规：** 仅修改 HeroAssets.asset、MonsterAssets.asset；CubeEntity 验证通过无需改动。
2. **构建验证：** `Assembly-CSharp.csproj` 0 errors。
3. **Plan Step 1–4 全覆盖：** 英雄 CC、怪物 Rigidbody Box、CubeEntity 验证、构建、提交。

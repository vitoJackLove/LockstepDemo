# Task 6 Report — PhysicsConfigConverter 运行时转换

## Contract

- **STATUS:** DONE
- **Commits:** `c18f9cf7153d45a55a288a99eb85c4dbae78596a`
- **Base:** `922441e`
- **Tests:**
  - `dotnet build Assembly-CSharp.csproj -nologo -v:minimal` — 0 errors
  - EditMode 测试 `PhysicsConfigConverterTests` 已按 plan 编写；需在 Unity Test Runner 中运行（`KCC.EditModeTests` 程序集）
- **Concerns:** 见下方

---

## Deliverables

| File | Description |
|------|-------------|
| `Assets/Scripts/RunTime/KCC/Physics/PhysicsConfigConverter.cs` | Inspector float/Vector3 → 运行时 fp 类型转换 |
| `Assets/Scripts/Test/EditMode/KCC/PhysicsConfigConverterTests.cs` | EditMode 单元测试（ToFp3 一致性、Motor 尺寸映射） |

## Implementation Summary

### PhysicsConfigConverter

- **`ToFp3(Vector3)`** — 委托 `fpmath1.Vector3ToFp3`，保证与项目既有定点量化规则一致。
- **`ToCharacterMotorDimensions(CharacterControllerSettings, out fp radius, out fp height, out fp yOffset)`** — 从 CC Authoring 配置提取胶囊半径、高度与 center.y 偏移。
- **`CreateCollider(PhysicsColliderSetting)`** — 按 `PhysicsShapeType` 创建 `FPBoxCollider` / `FPSphereCollider` / `FPCapsuleCollider` 实例（未注册到 `FPCollisionWorld`）；Box/Sphere 调用 `SetWorldPose` 应用本地偏移与欧拉角旋转。

### PhysicsConfigConverterTests

- `ToFp3_QuantizesConsistently` — 相同 Vector3 输入两次转换结果相等。
- `ToCharacterMotorDimensions_MapsCenterYOffset` — radius/height/yOffset 映射验证。

## Build Verification

```
dotnet build Assembly-CSharp.csproj -nologo -v:minimal
```

Result: **Build succeeded** — 0 errors，37 warnings（项目既有警告，与本次变更无关）。

## Commit

```
feat(kcc): add PhysicsConfigConverter for fp runtime
```

Hash: `c18f9cf7153d45a55a288a99eb85c4dbae78596a`

## Concerns

1. **`CreateCollider` 不注册 World：** 按 plan 设计，调用方负责后续注册；与 `KccRegistrationComponent` 集成在后续 Task。
2. **Capsule 分支未调用 `SetWorldPose`：** 与 plan 源码一致；`FPCapsuleCollider` 构造函数已接收 localCenter。
3. **EditMode 测试未在 CLI 执行：** `KCC.EditModeTests` 无独立 `.csproj`；需在 Unity Editor Test Runner 中验证 PASS。

## Self-Review

1. **范围合规：** 仅新增 plan 指定的 2 个 `.cs` 文件及对应 `.meta`。
2. **代码一致性：** 与 plan Step 3 源码一致。
3. **构建验证：** `Assembly-CSharp.csproj` 编译通过，无新增错误。

### Task 2: FPSphereCollider 实现

**Files:**
- Create: `Assets/Scripts/RunTime/KCC/Physics/FPSphereCollider.cs`
- Create: `Assets/Scripts/RunTime/KCC/Physics/FPSphereCollider.cs.meta`（Unity 自动生成，提交时一并纳入）
- Create: `Assets/Scripts/Test/EditMode/KCC/FPSphereCollisionTests.cs`

**Interfaces:**
- Consumes: `IFPCollider.GetSphereShape()`, `FPCollisionWorld.AllocateColliderId()`
- Produces: `FPSphereCollider` 类，公开 `SyncFromTransform(fp3 position, fpquaternion rotation, fp3 localCenter, fp baseRadius, fp3 scale)`

(See full steps in plan — implement TDD: write tests, implement FPSphereCollider per plan code, dotnet build, commit `feat(kcc): add FPSphereCollider`)

Note: `CapsuleIntersectsSphere_Overlapping_ReturnsTrue` may still FAIL until Task 4 — Task 2 only requires BUILD SUCCESS and first test passing after implementation.

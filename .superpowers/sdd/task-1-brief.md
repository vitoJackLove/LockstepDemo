### Task 1: FPSphereShape 与 IFPCollider 扩展

**Files:**
- Modify: `Assets/Scripts/RunTime/KCC/Physics/FPPhysicsTypes.cs`
- Modify: `Assets/Scripts/RunTime/KCC/Physics/IFPCollider.cs`
- Modify: `Assets/Scripts/RunTime/KCC/Physics/FPBoxCollider.cs`（补 `GetSphereShape` 默认实现）

**Interfaces:**
- Produces: `FPShapeType.Sphere = 3`
- Produces: `struct FPSphereShape { fp3 Center; fp Radius; }`
- Produces: `IFPCollider.GetSphereShape()`

- [ ] **Step 1: 扩展 FPPhysicsTypes**

在 `FPShapeType` 枚举 `Plane = 2` 之后追加：

```csharp
/// <summary>球体。</summary>
Sphere = 3,
```

在文件末尾追加：

```csharp
/// <summary>
/// 球体碰撞形状（世界空间）。
/// </summary>
public struct FPSphereShape
{
    /// <summary>球心世界坐标。</summary>
    public fp3 Center;

    /// <summary>球体半径。</summary>
    public fp Radius;
}
```

- [ ] **Step 2: 扩展 IFPCollider**

```csharp
/// <summary>
/// 获取球体形状数据。
/// </summary>
/// <returns>球体形状。</returns>
FPSphereShape GetSphereShape();
```

- [ ] **Step 3: FPBoxCollider 补默认实现**

```csharp
public FPSphereShape GetSphereShape() => default;
```

- [ ] **Step 4: 构建验证**

Run: `dotnet build Assembly-CSharp.csproj -nologo -v:minimal`  
Expected: BUILD SUCCESS（`FPCapsuleCollision` 等现有 switch 需补 `Sphere` case 或 default，本 Task 仅保证编译；Task 4 补全相交）

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/RunTime/KCC/Physics/FPPhysicsTypes.cs \
        Assets/Scripts/RunTime/KCC/Physics/IFPCollider.cs \
        Assets/Scripts/RunTime/KCC/Physics/FPBoxCollider.cs
git commit -m "feat(kcc): add FPSphereShape and IFPCollider sphere API"
```

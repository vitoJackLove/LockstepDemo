using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;

/// <summary>
/// 定点数 KCC ECS 系统，由 <see cref="RogueWorld"/> 注册到世界系统列表。
/// 在 BaseWorld.FixedUpdate 帧末按 WorldUpdateType 驱动 Simulate：
/// MoveComponent 写入 Motor 目标位姿 → Phase1/Phase2 碰撞校验 → 回写 BoundEntity.FTransform。
/// </summary>
public class FPKinematicCharacterSystem : BaseSystem
{
    /// <summary>当前正在执行模拟的系统实例。</summary>
    private static FPKinematicCharacterSystem _active;

    /// <summary>已注册的角色 Motor 列表。</summary>
    private readonly List<FPKinematicCharacterMotor> _characterMotors = new List<FPKinematicCharacterMotor>(16);

    /// <summary>已注册的运动学平台列表。</summary>
    private readonly List<FPPhysicsMover> _physicsMovers = new List<FPPhysicsMover>(8);

    /// <summary>当前活跃的系统实例（模拟期间非空）。</summary>
    internal static FPKinematicCharacterSystem Active => _active;

    /// <summary>已注册的角色 Motor 只读列表。</summary>
    internal IReadOnlyList<FPKinematicCharacterMotor> CharacterMotors => _characterMotors;

    /// <summary>
    /// 设置角色 Motor 列表容量。
    /// </summary>
    /// <param name="capacity">目标容量，不会小于当前已注册数量。</param>
    public void SetCharacterMotorsCapacity(int capacity)
    {
        if (capacity < _characterMotors.Count)
        {
            capacity = _characterMotors.Count;
        }

        _characterMotors.Capacity = capacity;
    }

    /// <summary>
    /// 注册角色 Motor。
    /// </summary>
    /// <param name="motor">待注册的 Motor，为 null 或已注册则忽略。</param>
    public void RegisterCharacterMotor(FPKinematicCharacterMotor motor)
    {
        if (motor != null && !_characterMotors.Contains(motor))
        {
            _characterMotors.Add(motor);
        }
    }

    /// <summary>
    /// 注销角色 Motor。
    /// </summary>
    /// <param name="motor">待注销的 Motor。</param>
    public void UnregisterCharacterMotor(FPKinematicCharacterMotor motor)
    {
        _characterMotors.Remove(motor);
    }

    /// <summary>
    /// 设置运动学平台列表容量。
    /// </summary>
    /// <param name="capacity">目标容量，不会小于当前已注册数量。</param>
    public void SetPhysicsMoversCapacity(int capacity)
    {
        if (capacity < _physicsMovers.Count)
        {
            capacity = _physicsMovers.Count;
        }

        _physicsMovers.Capacity = capacity;
    }

    /// <summary>
    /// 注册运动学平台。
    /// </summary>
    /// <param name="mover">待注册的平台，为 null 或已注册则忽略。</param>
    public void RegisterPhysicsMover(FPPhysicsMover mover)
    {
        if (mover != null && !_physicsMovers.Contains(mover))
        {
            _physicsMovers.Add(mover);
        }
    }

    /// <summary>
    /// 注销运动学平台。
    /// </summary>
    /// <param name="mover">待注销的平台。</param>
    public void UnregisterPhysicsMover(FPPhysicsMover mover)
    {
        _physicsMovers.Remove(mover);
    }

    /// <summary>
    /// 世界固定帧回调：在 BaseWorld.FixedUpdate 中于实体组件更新之后执行 KCC 模拟。
    /// 委托 <see cref="Simulate"/> 完成平台速度更新、Motor 两阶段解算与变换回写。
    /// </summary>
    /// <param name="deltaTime">逻辑帧间隔（定点）。</param>
    /// <param name="worldUpdateType">本地预测 / 权威校正 / 回滚重放类型。</param>
    public override void OnFixedUpdate(fp deltaTime, WorldUpdateType worldUpdateType)
    {
        base.OnFixedUpdate(deltaTime, worldUpdateType);

        Simulate(deltaTime, worldUpdateType);
    }

    /// <summary>
    /// 执行 KCC 模拟并将校验后的变换回写实体。
    /// </summary>
    /// <param name="deltaTime">帧间隔时间。</param>
    /// <param name="worldUpdateType">世界更新类型（本地/权威/回滚）。</param>
    /// <param name="simulateAllMotors">为 true 时忽略实体更新类型，模拟所有 Motor。</param>
    public void Simulate(fp deltaTime, WorldUpdateType worldUpdateType, bool simulateAllMotors = false)
    {
        _active = this;
        try
        {
            Simulate(deltaTime, _characterMotors, _physicsMovers, worldUpdateType, simulateAllMotors);
        }
        finally
        {
            if (_active == this)
            {
                _active = null;
            }
        }
    }

    /// <summary>
    /// 世界销毁时清空已注册 Motor 与平台列表，并释放 <see cref="Active"/> 静态引用。
    /// </summary>
    public override void OnDispose()
    {
        _characterMotors.Clear();
        _physicsMovers.Clear();

        if (_active == this)
        {
            _active = null;
        }

        base.OnDispose();
    }

    /// <summary>
    /// 核心模拟流程：平台速度更新 → Motor Phase1 → 平台位姿应用 → Motor Phase2 → 回写变换。
    /// </summary>
    /// <param name="deltaTime">帧间隔时间。</param>
    /// <param name="motors">角色 Motor 列表。</param>
    /// <param name="movers">运动学平台列表。</param>
    /// <param name="worldUpdateType">世界更新类型。</param>
    /// <param name="simulateAllMotors">是否模拟所有 Motor。</param>
    private static void Simulate(
        fp deltaTime,
        List<FPKinematicCharacterMotor> motors,
        List<FPPhysicsMover> movers,
        WorldUpdateType worldUpdateType,
        bool simulateAllMotors)
    {
        int characterMotorsCount = motors.Count;
        int physicsMoversCount = movers.Count;
        // 仅在本地更新时模拟运动学平台
        bool simulateMovers = worldUpdateType == WorldUpdateType.Local;

        if (simulateMovers)
        {
            for (int i = 0; i < physicsMoversCount; i++)
            {
                movers[i].VelocityUpdate(deltaTime);
            }
        }

        // Phase1：地面检测、速度/旋转更新等
        for (int i = 0; i < characterMotorsCount; i++)
        {
            FPKinematicCharacterMotor motor = motors[i];
            if (!ShouldSimulateMotor(motor, worldUpdateType, simulateAllMotors))
            {
                continue;
            }

            motor.UpdatePhase1(deltaTime);
        }

        if (simulateMovers)
        {
            for (int i = 0; i < physicsMoversCount; i++)
            {
                movers[i].ApplySimulationPose();
            }
        }

        // Phase2：移动 sweep、碰撞解算等
        for (int i = 0; i < characterMotorsCount; i++)
        {
            FPKinematicCharacterMotor motor = motors[i];
            if (!ShouldSimulateMotor(motor, worldUpdateType, simulateAllMotors))
            {
                continue;
            }

            motor.UpdatePhase2(deltaTime);
        }

        ApplyValidatedTransforms(motors, worldUpdateType, simulateAllMotors);
    }

    /// <summary>
    /// 根据世界更新类型与实体类型判断 Motor 是否参与本帧模拟。
    /// </summary>
    /// <param name="motor">角色 Motor。</param>
    /// <param name="worldUpdateType">世界更新类型。</param>
    /// <param name="simulateAllMotors">是否强制模拟所有 Motor。</param>
    /// <returns>若应参与模拟则返回 true。</returns>
    private static bool ShouldSimulateMotor(FPKinematicCharacterMotor motor, WorldUpdateType worldUpdateType, bool simulateAllMotors)
    {
        if (simulateAllMotors || motor == null)
        {
            return motor != null;
        }

        BaseEntity entity = motor.BoundEntity;
        if (entity == null)
        {
            return true;
        }

        // 权威实体仅在权威更新时模拟
        if (entity.EntityUpdateType == EntityUpdateType.AuthorityEntity)
        {
            return worldUpdateType == WorldUpdateType.Authority;
        }

        return worldUpdateType == WorldUpdateType.Local || worldUpdateType == WorldUpdateType.RollBack;
    }

    /// <summary>
    /// 将 Motor 校验后的瞬态位姿回写到绑定实体的 FTransform。
    /// </summary>
    /// <param name="motors">角色 Motor 列表。</param>
    /// <param name="worldUpdateType">世界更新类型。</param>
    /// <param name="simulateAllMotors">是否强制模拟所有 Motor。</param>
    private static void ApplyValidatedTransforms(
        List<FPKinematicCharacterMotor> motors,
        WorldUpdateType worldUpdateType,
        bool simulateAllMotors)
    {
        for (int i = 0; i < motors.Count; i++)
        {
            FPKinematicCharacterMotor motor = motors[i];
            if (!ShouldSimulateMotor(motor, worldUpdateType, simulateAllMotors))
            {
                continue;
            }

            BaseEntity entity = motor.BoundEntity;
            if (entity?.transform == null)
            {
                continue;
            }

            PhysicsMotionInfluence influence = motor.CollisionInfluence ?? PhysicsMotionInfluence.CreateCharacterControllerDefault();
            fp3 currentPosition = entity.transform.Position;
            fpquaternion currentRotation = entity.transform.Rotation;
            entity.transform.Position = influence.BlendPosition(currentPosition, motor.TransientPosition);
            entity.transform.Rotation = influence.BlendRotation(currentRotation, motor.TransientRotation);
        }
    }
}

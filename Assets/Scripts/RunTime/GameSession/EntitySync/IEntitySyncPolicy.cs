using System.Collections.Generic;

/// <summary>
/// 实体同步策略：定义 Local/Authority 实体的注册、命令绑定与调试操作目标解析。
/// </summary>
public interface IEntitySyncPolicy
{
    /// <summary>
    /// 注册主角实体。联机传入 Local + Authority 两个实体，单机可只传主实体。
    /// </summary>
    void RegisterActor(EntitySystem entitySystem, BaseEntity localEntity, BaseEntity authorityEntity);

    /// <summary>
    /// 获取当前帧命令应绑定的实体。
    /// </summary>
    BaseEntity GetCommandEntity(EntitySystem entitySystem);

    /// <summary>
    /// 解析金手指/调试工具的操作目标列表。
    /// </summary>
    bool TryResolveOperationTargets(
        BaseEntity selectedEntity,
        EntitySystem entitySystem,
        out IReadOnlyList<BaseEntity> targets,
        out string failureMessage);
}

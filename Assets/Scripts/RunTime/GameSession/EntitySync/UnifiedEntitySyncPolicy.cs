using System.Collections.Generic;

/// <summary>
/// 统一实体同步策略：单机模式下同一实体同时承担 Local 与 Authority 角色。
/// </summary>
public sealed class UnifiedEntitySyncPolicy : IEntitySyncPolicy
{
    /// <inheritdoc />
    public void RegisterActor(EntitySystem entitySystem, BaseEntity localEntity, BaseEntity authorityEntity)
    {
        BaseEntity primaryEntity = localEntity ?? authorityEntity;
        entitySystem.RegisterActor(primaryEntity, primaryEntity);
    }

    /// <inheritdoc />
    public BaseEntity GetCommandEntity(EntitySystem entitySystem)
    {
        return entitySystem.ActorAuthorityEntity ?? entitySystem.ActorLocalEntity;
    }

    /// <inheritdoc />
    public bool TryResolveOperationTargets(
        BaseEntity selectedEntity,
        EntitySystem entitySystem,
        out IReadOnlyList<BaseEntity> targets,
        out string failureMessage)
    {
        targets = null;
        failureMessage = null;

        if (selectedEntity == null)
        {
            failureMessage = "实体引用为空";
            return false;
        }

        if (entitySystem == null)
        {
            failureMessage = "当前世界未找到实体系统";
            return false;
        }

        if (!entitySystem.ContainsExecutingEntity(selectedEntity))
        {
            failureMessage = "实体已不在当前世界中";
            return false;
        }

        // 单机只有一套实体，选中即唯一操作目标，避免重复写入同一实体两次
        if (entitySystem.IsActorEntity(selectedEntity))
        {
            BaseEntity actorEntity = entitySystem.ActorLocalEntity ?? entitySystem.ActorAuthorityEntity;
            targets = new[] { actorEntity };
            return true;
        }

        targets = new[] { selectedEntity };
        return true;
    }
}

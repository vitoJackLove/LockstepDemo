using System.Collections.Generic;

/// <summary>
/// 双实体同步策略：联机模式下 Local 与 Authority 分别维护，操作需成对同步。
/// </summary>
public sealed class DualEntitySyncPolicy : IEntitySyncPolicy
{
    /// <inheritdoc />
    public void RegisterActor(EntitySystem entitySystem, BaseEntity localEntity, BaseEntity authorityEntity)
    {
        entitySystem.RegisterActor(authorityEntity, localEntity);
    }

    /// <inheritdoc />
    public BaseEntity GetCommandEntity(EntitySystem entitySystem)
    {
        return entitySystem.ActorAuthorityEntity;
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

        if (selectedEntity.ForecastEntityType == ForecastEntityType.DynamicEntity)
        {
            return TryResolveDynamicTargets(selectedEntity, entitySystem, out targets, out failureMessage);
        }

        if (selectedEntity.EntityUpdateType == EntityUpdateType.LocalEntity &&
            selectedEntity == entitySystem.ActorLocalEntity)
        {
            if (entitySystem.ActorAuthorityEntity == null)
            {
                failureMessage = "无法解析本地预测实体对应的权威实体：主角权威实体不可用";
                return false;
            }

            targets = new[] { selectedEntity, entitySystem.ActorAuthorityEntity };
            return true;
        }

        if (selectedEntity.EntityUpdateType == EntityUpdateType.AuthorityEntity &&
            selectedEntity == entitySystem.ActorAuthorityEntity)
        {
            if (entitySystem.ActorLocalEntity == null)
            {
                failureMessage = "无法解析权威实体对应的本地预测实体：主角本地预测实体不可用";
                return false;
            }

            targets = new[] { selectedEntity, entitySystem.ActorLocalEntity };
            return true;
        }

        targets = new[] { selectedEntity };
        return true;
    }

    /// <summary>
    /// 按指纹解析动态实体的 Local/Authority 配对目标。
    /// </summary>
    private static bool TryResolveDynamicTargets(
        BaseEntity selectedEntity,
        EntitySystem entitySystem,
        out IReadOnlyList<BaseEntity> targets,
        out string failureMessage)
    {
        targets = null;
        BaseEntity localEntity = selectedEntity.EntityUpdateType == EntityUpdateType.LocalEntity
            ? selectedEntity
            : entitySystem.GetDynamicLocalEntity<BaseEntity>(selectedEntity.EntityFingerprints);
        BaseEntity authorityEntity = selectedEntity.EntityUpdateType == EntityUpdateType.AuthorityEntity
            ? selectedEntity
            : entitySystem.GetDynamicAuthorityEntity<BaseEntity>(selectedEntity.EntityFingerprints);

        if (localEntity == null)
        {
            failureMessage = "无法解析对应动态本地预测实体";
            return false;
        }

        if (authorityEntity == null)
        {
            failureMessage = "无法解析对应动态权威实体";
            return false;
        }

        targets = new[] { localEntity, authorityEntity };
        failureMessage = null;
        return true;
    }
}

using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;

/// <summary>
/// 怪物锁定组件。
/// 只维护该帧锁定到的位置和旋转，不驱动实体移动、转向或释放技能。
/// 锁定数据不进入快照系统；权威更新后由 EntitySystem 旁路覆盖预测怪物。
/// </summary>
public class EnemyDetectionComponent : BaseComponent
{
    private bool _hasLockedTarget;
    private fp3 _lockedPosition;
    private fpquaternion _lockedRotation = fpquaternion.identity;
    private uint _lockedTick;

    public bool HasLockedTarget => _hasLockedTarget;
    public fp3 LockedPosition => _lockedPosition;
    public fpquaternion LockedRotation => _lockedRotation;
    public uint LockedTick => _lockedTick;

    public void ApplyAuthorityLockFrom(EnemyDetectionComponent authorityComponent)
    {
        if (authorityComponent == null)
        {
            return;
        }

        _hasLockedTarget = authorityComponent._hasLockedTarget;
        _lockedPosition = authorityComponent._lockedPosition;
        _lockedRotation = authorityComponent._lockedRotation;
        _lockedTick = authorityComponent._lockedTick;
    }

    public void UpdateLock(uint tick, WorldUpdateType worldUpdateType)
    {
        _lockedTick = tick;

        if (!TryFindNearestEnemy(worldUpdateType, out fp3 targetPosition))
        {
            ClearLock(tick);
            return;
        }

        _hasLockedTarget = true;
        _lockedPosition = targetPosition;

        fp3 dir = targetPosition - Entity.transform.Position;
        _lockedRotation = (dir == fp3.zero).Bool3ToBool()
            ? fpquaternion.identity
            : fpmath1.LookRotation(dir, fpmath1.up());
    }

    private void ClearLock(uint tick)
    {
        _hasLockedTarget = false;
        _lockedTick = tick;
        _lockedPosition = fp3.zero;
        _lockedRotation = fpquaternion.identity;
    }

    private bool TryFindNearestEnemy(WorldUpdateType worldUpdateType, out fp3 targetPosition)
    {
        targetPosition = fp3.zero;

        if (Entity == null)
        {
            return false;
        }

        fp detectionDistance = GetDetectionDistance();
        if (detectionDistance <= (fp)0)
        {
            return false;
        }

        IReadOnlyList<BaseEntity> candidates = Entity.GetSystem<EntitySystem>().GetLockTargetCandidates(worldUpdateType);
        fp minDistance = detectionDistance;
        int selectedEntityId = int.MaxValue;
        bool hasTarget = false;

        for (int i = 0; i < candidates.Count; i++)
        {
            BaseEntity candidate = candidates[i];

            if (!IsValidCandidate(candidate))
            {
                continue;
            }

            fp distance = fpmath.distance(Entity.transform.Position, candidate.transform.Position);

            if (distance > detectionDistance)
            {
                continue;
            }

            if (!hasTarget || distance < minDistance ||
                (distance == minDistance && candidate.EntityId < selectedEntityId))
            {
                hasTarget = true;
                minDistance = distance;
                selectedEntityId = candidate.EntityId;
                targetPosition = candidate.transform.Position;
            }
        }

        return hasTarget;
    }

    private bool IsValidCandidate(BaseEntity candidate)
    {
        if (candidate == null || candidate == Entity)
        {
            return false;
        }

        if (candidate.EntityState != EntityState.Survival)
        {
            return false;
        }

        return Entity.CheckIsAdversarial(candidate);
    }

    private fp GetDetectionDistance()
    {
        if (Entity is MonsterEntity monsterEntity && monsterEntity.BattleMonsterData != null)
        {
            return (fp)monsterEntity.BattleMonsterData.MonsterAssetsConfig.attackDistance;
        }

        return (fp)0;
    }
}

using System.Collections.Generic;

/// <summary>
/// KCC 碰撞世界单例。收集 Authoring 注册的 IFPCollider，供 FPPhysicsQuery 遍历查询。
/// Motor.RebuildCollidableLayers 通过本类的层矩阵决定角色可碰撞层。
/// </summary>
public sealed class FPCollisionWorld
{
    /// <summary>单例实例。</summary>
    private static FPCollisionWorld _instance;

    /// <summary>碰撞世界单例访问器。</summary>
    public static FPCollisionWorld Instance => _instance ??= new FPCollisionWorld();

    /// <summary>已注册的碰撞体列表。</summary>
    private readonly List<IFPCollider> _colliders = new List<IFPCollider>(256);

    /// <summary>层间碰撞矩阵（按层索引存储）。</summary>
    private readonly Dictionary<int, bool[,]> _layerCollisionMatrix = new Dictionary<int, bool[,]>();

    /// <summary>下一个碰撞体 ID。</summary>
    private int _nextColliderId = 1;

    /// <summary>下一个刚体 ID。</summary>
    private int _nextBodyId = 1;

    /// <summary>下一个运动学平台 ID。</summary>
    private int _nextMoverId = 1;

    /// <summary>已注册碰撞体的只读列表。</summary>
    public IReadOnlyList<IFPCollider> Colliders => _colliders;

    /// <summary>
    /// 重置碰撞世界：清空碰撞体并重置 ID 计数器与默认层矩阵。
    /// </summary>
    public void Reset()
    {
        _colliders.Clear();
        _nextColliderId = 1;
        _nextBodyId = 1;
        _nextMoverId = 1;
        InitializeDefaultLayerMatrix();
    }

    /// <summary>
    /// 构造碰撞世界并初始化默认层碰撞矩阵。
    /// </summary>
    public FPCollisionWorld()
    {
        InitializeDefaultLayerMatrix();
    }

    /// <summary>
    /// 重新初始化默认层碰撞矩阵。
    /// </summary>
    public void InitializeDefaultLayerMatrix()
    {
        _layerCollisionMatrix.Clear();
        SetDefaultLayerCollisions();
    }

    /// <summary>
    /// 配置项目默认的层间碰撞规则。
    /// </summary>
    private void SetDefaultLayerCollisions()
    {
        SetLayersCollide(FPCollisionLayer.Default, FPCollisionLayer.Wall, true);
        SetLayersCollide(FPCollisionLayer.Default, FPCollisionLayer.KinematicPlatform, true);
        SetLayersCollide(FPCollisionLayer.Default, FPCollisionLayer.DynamicBody, true);
        SetLayersCollide(FPCollisionLayer.Hero, FPCollisionLayer.Default, true);
        SetLayersCollide(FPCollisionLayer.Hero, FPCollisionLayer.Wall, true);
        SetLayersCollide(FPCollisionLayer.Hero, FPCollisionLayer.KinematicPlatform, true);
        SetLayersCollide(FPCollisionLayer.Hero, FPCollisionLayer.DynamicBody, true);
        SetLayersCollide(FPCollisionLayer.Hero, FPCollisionLayer.Monster, true);
        SetLayersCollide(FPCollisionLayer.Monster, FPCollisionLayer.Wall, true);
        SetLayersCollide(FPCollisionLayer.Monster, FPCollisionLayer.Default, true);
        SetLayersCollide(FPCollisionLayer.Monster, FPCollisionLayer.KinematicPlatform, true);
        SetLayersCollide(FPCollisionLayer.Monster, FPCollisionLayer.DynamicBody, true);
        // 英雄之间默认不碰撞
        SetLayersCollide(FPCollisionLayer.Hero, FPCollisionLayer.Hero, false);
    }

    /// <summary>
    /// 分配新的碰撞体 ID。
    /// </summary>
    /// <returns>唯一碰撞体 ID。</returns>
    public int AllocateColliderId() => _nextColliderId++;

    /// <summary>
    /// 分配新的刚体 ID。
    /// </summary>
    /// <returns>唯一刚体 ID。</returns>
    public int AllocateBodyId() => _nextBodyId++;

    /// <summary>
    /// 分配新的运动学平台 ID。
    /// </summary>
    /// <returns>唯一平台 ID。</returns>
    public int AllocateMoverId() => _nextMoverId++;

    /// <summary>
    /// 注册碰撞体到世界。
    /// </summary>
    /// <param name="collider">待注册碰撞体，为 null 或已注册则忽略。</param>
    public void RegisterCollider(IFPCollider collider)
    {
        if (collider == null || _colliders.Contains(collider))
        {
            return;
        }

        _colliders.Add(collider);
    }

    /// <summary>
    /// 从世界注销碰撞体。
    /// </summary>
    /// <param name="collider">待注销碰撞体。</param>
    public void UnregisterCollider(IFPCollider collider)
    {
        _colliders.Remove(collider);
    }

    /// <summary>
    /// 设置两层之间是否发生碰撞。
    /// </summary>
    /// <param name="a">碰撞层 A。</param>
    /// <param name="b">碰撞层 B。</param>
    /// <param name="shouldCollide">是否碰撞。</param>
    public void SetLayersCollide(FPCollisionLayer a, FPCollisionLayer b, bool shouldCollide)
    {
        int ia = LayerToIndex(a);
        int ib = LayerToIndex(b);
        EnsureMatrixSize(ia);
        EnsureMatrixSize(ib);
        _layerCollisionMatrix[ia][ia, ib] = shouldCollide;
        _layerCollisionMatrix[ib][ib, ia] = shouldCollide;
    }

    /// <summary>
    /// 查询两层之间是否发生碰撞。
    /// </summary>
    /// <param name="a">碰撞层 A。</param>
    /// <param name="b">碰撞层 B。</param>
    /// <returns>若应碰撞则返回 true；任一层为 None 则返回 false。</returns>
    public bool GetLayersCollide(FPCollisionLayer a, FPCollisionLayer b)
    {
        if (a == FPCollisionLayer.None || b == FPCollisionLayer.None)
        {
            return false;
        }

        int ia = LayerToIndex(a);
        int ib = LayerToIndex(b);
        if (!_layerCollisionMatrix.TryGetValue(ia, out bool[,] matrix))
        {
            return true;
        }

        return matrix[ia, ib];
    }

    /// <summary>
    /// 将 Flags 枚举碰撞层转换为矩阵索引（位序号）。
    /// </summary>
    /// <param name="layer">碰撞层。</param>
    /// <returns>矩阵索引。</returns>
    private static int LayerToIndex(FPCollisionLayer layer)
    {
        int value = (int)layer;
        int index = 0;
        while (value > 1)
        {
            value >>= 1;
            index++;
        }

        return index;
    }

    /// <summary>
    /// 确保指定层索引的碰撞矩阵已分配（默认全部可碰撞）。
    /// </summary>
    /// <param name="index">层索引。</param>
    private void EnsureMatrixSize(int index)
    {
        if (_layerCollisionMatrix.ContainsKey(index))
        {
            return;
        }

        const int size = 16;
        bool[,] matrix = new bool[size, size];
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                matrix[i, j] = true;
            }
        }

        _layerCollisionMatrix[index] = matrix;
    }
}

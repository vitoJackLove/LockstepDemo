using System.Collections.Generic;
using PrimitiveDetection;

/// <summary>
/// 受击盒实体接口
/// </summary>
public interface IColliderEntityView
{
     Dictionary<string, HitColliderEditorSetting> Primitives { get; }
}

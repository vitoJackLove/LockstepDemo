using System;
using Sirenix.OdinInspector;

[Serializable]
public class EntityAssetsConfig 
{
     [LabelText("ID")]
     public int assetsId;
    
     [LabelText("资源路径")]
     public string assetsPath;
}

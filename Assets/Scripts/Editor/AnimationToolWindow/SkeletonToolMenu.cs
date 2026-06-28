using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class SkeletonToolMenu : OdinMenuEditorWindow
{
    [MenuItem("Tools/骨骼世界坐标曲线")]
    private static void OpenWindow()
    {
        GetWindow<SkeletonToolMenu>().Show();
    }

    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();

        tree.Add("骨骼曲线配置界面", new SkeletonToolWindow());
        
        tree.Add("骨骼曲线列表", new EntitySkeletonCurveListWindow());
        
        tree.Add("测试骨骼曲线", new MeasurementSkeletonCurveWindow());
        
        return tree;
    }
}
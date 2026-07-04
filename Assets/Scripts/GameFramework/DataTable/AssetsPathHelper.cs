

public static class AssetsPathHelper 
{
    public static string UIWindowPathHelper(string windowName)
    {
        return $"Assets/Prefabs/UI/Window/{windowName}.prefab";
    }

    public static string GameAssetsConfigHelper(string assetsConfig)
    {
        return $"Assets/GameAssetConfig/{assetsConfig}.asset";
    }
    
    public static string EntityPathHelper(string entityName)
    {
        if (entityName.StartsWith("Assets/"))
        {
            return entityName.EndsWith(".prefab") ? entityName : $"{entityName}.prefab";
        }

        return $"Assets/Prefabs/Battle/{entityName}.prefab";
    }
    
    public static string NavMeshPathHelper(string navMeshName)
    {
        return $"Assets/Prefabs/NavMesh/{navMeshName}.prefab";
    }
    
    public static string LoadScenePathHelper(string sceneName)
    {
        return $"Assets/Scene/{sceneName}.unity";
    }
    
    public static string LoadMapCellPathHelper(string mapCellName)
    {
        return $"Assets/Prefabs/Map/MapCube/{mapCellName}.prefab";
    }

    public static string LoadGameConfig(string configName)
    {
        return $"Assets/Config/{configName}.asset";
    }
}

using System;

namespace Rogue.Bootstrap.HotUpdate
{
    /// <summary>
    /// 热更代码 Addressables 资产路径常量。
    /// </summary>
    public static class HotUpdateCodePaths
    {
        public const string Root = "Assets/HotUpdate/Code";
        public const string DefaultRuntimeAssemblyAddress = Root + "/Game.Runtime.dll.bytes";
        public const string DefaultAotMetadataLabel = "hotupdate-aot";
        public const string AotMetadataSubFolder = Root + "/AOT";

        public static string GetAotMetadataAddress(string assemblyName)
        {
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                throw new ArgumentException("程序集名称不能为空。", nameof(assemblyName));
            }

            string normalizedName = assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                ? assemblyName
                : $"{assemblyName}.dll";
            return $"{AotMetadataSubFolder}/{normalizedName}.bytes";
        }
    }
}

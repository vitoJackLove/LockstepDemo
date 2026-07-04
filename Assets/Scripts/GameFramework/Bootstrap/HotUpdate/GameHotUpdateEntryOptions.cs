using System;



namespace Rogue.Bootstrap.HotUpdate

{

    /// <summary>

    /// 代码热更入口配置，作为不可变值对象在加载流程中传递。

    /// </summary>

    public sealed class GameHotUpdateEntryOptions

    {

        public GameHotUpdateEntryOptions(

            string hotUpdateAssemblyName,

            string hotUpdateAssemblyAddress,

            string aotMetadataLabel,

            bool autoDiscoverAotAssemblies,

            string[] aotAssemblyNames)

        {

            if (string.IsNullOrWhiteSpace(hotUpdateAssemblyName))

            {

                throw new ArgumentException("热更新程序集名称不能为空。", nameof(hotUpdateAssemblyName));

            }



            if (string.IsNullOrWhiteSpace(hotUpdateAssemblyAddress))

            {

                throw new ArgumentException("热更新程序集 Address 不能为空。", nameof(hotUpdateAssemblyAddress));

            }



            if (string.IsNullOrWhiteSpace(aotMetadataLabel))

            {

                throw new ArgumentException("AOT 元数据 Label 不能为空。", nameof(aotMetadataLabel));

            }



            HotUpdateAssemblyName = hotUpdateAssemblyName;

            HotUpdateAssemblyAddress = hotUpdateAssemblyAddress;

            AotMetadataLabel = aotMetadataLabel;

            AutoDiscoverAotAssemblies = autoDiscoverAotAssemblies;

            AotAssemblyNames = aotAssemblyNames ?? Array.Empty<string>();

        }



        /// <summary>

        /// 热更新主程序集名称，不包含 .dll 后缀（Editor 复用程序集时使用）。

        /// </summary>

        public string HotUpdateAssemblyName { get; }



        /// <summary>

        /// 热更新主程序集 Addressables Address。

        /// </summary>

        public string HotUpdateAssemblyAddress { get; }



        /// <summary>

        /// AOT 元数据批量加载 Label。

        /// </summary>

        public string AotMetadataLabel { get; }



        /// <summary>

        /// 是否通过 Label 自动发现 AOT 元数据 dll。

        /// </summary>

        public bool AutoDiscoverAotAssemblies { get; }



        /// <summary>

        /// 手动指定的 AOT 元数据程序集名称列表。

        /// </summary>

        public string[] AotAssemblyNames { get; }

    }

}



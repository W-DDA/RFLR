using System.Collections.Generic;

namespace MyLauncher.Core.Mod
{
    /// <summary>
    /// 一个已安装 Mod 的元数据。
    /// </summary>
    public class ModInfo
    {
        public string FileName { get; set; } = "";
        public string ModId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
        public string LoaderType { get; set; } = "";  // "fabric" / "forge" / "unknown"

        /// <summary>依赖：modId -> 版本要求</summary>
        public Dictionary<string, string> Depends { get; set; } = new();

        /// <summary>不兼容：modId -> 说明</summary>
        public Dictionary<string, string> Breaks { get; set; } = new();

        /// <summary>声明的 MC 版本</summary>
        public List<string> MinecraftVersions { get; set; } = new();
    }
}
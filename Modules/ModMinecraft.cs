using System.Collections.Generic;
using System.IO;

namespace MyLauncher.Modules
{
    /// <summary>
    /// 版本管理业务类，模仿 PCL2 的 Modules/Minecraft。
    /// UI 层只调用这里的方法，不直接操作文件系统。
    /// </summary>
    public static class ModMinecraft
    {
        /// <summary>
        /// 扫描指定游戏目录下的 versions 文件夹，
        /// 返回所有“结构完整”（文件夹内有同名 .json）的本地版本名。
        /// </summary>
        public static List<string> GetLocalVersions(string gameDir)
        {
            var result = new List<string>();
            var versionsDir = Path.Combine(gameDir, "versions");

            if (!Directory.Exists(versionsDir))
                return result;

            foreach (var dir in Directory.GetDirectories(versionsDir))
            {
                var name = Path.GetFileName(dir);
                var jsonPath = Path.Combine(dir, $"{name}.json");
                if (File.Exists(jsonPath))
                    result.Add(name);
            }

            return result;
        }

        /// <summary>
        /// 判断某个版本在本地是否已完整安装。
        /// </summary>
        public static bool IsVersionInstalled(string gameDir, string versionName)
        {
            var versionsDir = Path.Combine(gameDir, "versions");
            var jsonPath = Path.Combine(versionsDir, versionName, $"{versionName}.json");
            return File.Exists(jsonPath);
        }
    }
}
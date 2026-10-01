using System.IO;

namespace MyLauncher.Core.Launch
{
    public class GameDirManager
    {
        /// <summary>
        /// 解析游戏目录（支持版本隔离）。
        /// </summary>
        public string ResolveGameDir(string root, string versionId, bool isolate)
        {
            return isolate ? Path.Combine(root, "versions", versionId) : root;
        }

        public string ResolveAssets(string root) => Path.Combine(root, "assets");
        public string ResolveLibraries(string root) => Path.Combine(root, "libraries");
        public string ResolveNatives(string gameDir, string versionId) =>
            Path.Combine(gameDir, "natives-" + versionId);
    }
}
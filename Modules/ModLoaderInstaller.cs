using System;
using System.Net.Http;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Installer.Forge;

namespace MyLauncher.Modules
{
    public static class ModLoaderInstaller
    {
        public static async Task<string> InstallForgeAsync(string gameDir, string mcVersion)
        {
            var path = new MinecraftPath(gameDir);
            var launcher = new MinecraftLauncher(path);
            var installer = new ForgeInstaller(launcher);

            return await installer.Install(mcVersion, new ForgeInstallOptions());
        }

        public static Task<string> InstallFabricAsync(string gameDir, string mcVersion)
        {
            throw new NotSupportedException(
                "Fabric 安装功能已迁移到 Core.Version.LoaderInstaller。");
        }
    }
}
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MyLauncher.Core.Download;

namespace MyLauncher.Core.Version
{
    public class LoaderInstaller
    {
        private readonly string _root;
        private readonly HttpEngine _http = new();

        public LoaderInstaller(string root)
        {
            _root = root;
        }

        /// <summary>
        /// 安装 Fabric。走官方 meta API 获取 profile JSON，直接写入 versions 文件夹。
        /// </summary>
        public async Task<string> InstallFabricAsync(string mcVersion, string loaderVersion)
        {
            var url = $"https://meta.fabricmc.net/v2/versions/loader/{mcVersion}/{loaderVersion}/profile/json";
            var json = await ReadAllAsync(url);
            var id = $"fabric-loader-{loaderVersion}-{mcVersion}";

            var dir = Path.Combine(_root, "versions", id);
            Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(Path.Combine(dir, id + ".json"), json, Encoding.UTF8);

            return id;
        }

        /// <summary>
        /// 安装 Forge。下载 installer jar，然后用 ForgeProcessorExecutor 执行。
        /// </summary>
        public async Task<string> InstallForgeAsync(string mcVersion, string forgeVersion, string javaPath)
        {
            var full = $"{mcVersion}-{forgeVersion}";
            var url = $"https://maven.minecraftforge.net/net/minecraftforge/forge/{full}/forge-{full}-installer.jar";

            var installerDir = Path.Combine(_root, "installers");
            Directory.CreateDirectory(installerDir);
            var jar = Path.Combine(installerDir, $"forge-{full}-installer.jar");

            await _http.DownloadToFileAsync(url, jar, 0, null, RetryPolicy.Defaults);

            var executor = new ForgeProcessorExecutor();
            await executor.InstallAsync(jar, _root, javaPath);

            return full;
        }

        private async Task<string> ReadAllAsync(string url)
        {
            using var client = new HttpClient();
            return await client.GetStringAsync(url);
        }
    }
}
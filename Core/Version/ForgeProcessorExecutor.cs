using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MyLauncher.Core.Download;
using MyLauncher.Core.Util;

namespace MyLauncher.Core.Version
{
    public class ForgeProcessorExecutor
    {
        public async Task InstallAsync(string installerJar, string minecraftDir, string javaPath)
        {
            var librariesDir = Path.Combine(minecraftDir, "libraries");

            // 1. 从 installer JAR 提取 install_profile.json 和 version.json
            InstallProfile profile;
            VersionJson? versionJson;

            using (var zip = ZipFile.OpenRead(installerJar))
            {
                var profileEntry = zip.GetEntry("install_profile.json")
                    ?? throw new IOException("install_profile.json not found in installer");

                using (var stream = profileEntry.Open())
                    profile = JsonUtil.Read<InstallProfile>(stream);

                var versionEntry = zip.GetEntry("version.json");
                if (versionEntry != null)
                {
                    using var stream = versionEntry.Open();
                    versionJson = JsonUtil.Read<VersionJson>(stream);
                }
                else versionJson = null;
            }

            // 2. 下载 processor 所需的库
            await DownloadProcessorLibrariesAsync(profile, librariesDir);

            // 3. 构建 Processor 上下文
            var ctx = new ProcessorContext
            {
                RootDirectory = minecraftDir,
                InstallerJar = installerJar,
                LibrariesDir = librariesDir,
                MinecraftJar = Path.Combine(minecraftDir, "versions", profile.Minecraft, profile.Minecraft + ".jar")
            };

            // 4. 按序执行 processors
            if (profile.Processors != null)
            {
                foreach (var processor in profile.Processors)
                {
                    if (processor.Sides != null && !processor.Sides.Contains("client"))
                        continue;

                    await ExecuteProcessorAsync(processor, profile, ctx, javaPath);
                }
            }
        }

        private async Task DownloadProcessorLibrariesAsync(InstallProfile profile, string librariesDir)
        {
            var tasks = new List<DownloadTask>();
            if (profile.Processors == null) return;

            foreach (var p in profile.Processors)
            {
                if (!string.IsNullOrEmpty(p.Jar))
                    tasks.Add(MavenToTask(p.Jar, librariesDir));
                if (p.Classpath != null)
                    foreach (var cp in p.Classpath)
                        tasks.Add(MavenToTask(cp, librariesDir));
            }

            var dm = new DownloadManager(4);
            await dm.SubmitAsync(tasks);
        }

        private DownloadTask MavenToTask(string coordinate, string librariesDir)
        {
            var parts = coordinate.Split(':');
            var group = parts[0].Replace('.', '/');
            var artifact = parts[1];
            var version = parts[2].Split('@')[0];
            var ext = parts[2].Contains('@') ? parts[2].Split('@')[1] : "jar";

            var fileName = $"{artifact}-{version}.{ext}";
            var target = Path.Combine(librariesDir, group, artifact, version, fileName);
            var url = $"https://maven.minecraftforge.net/{group}/{artifact}/{version}/{fileName}";

            return new DownloadTask
            {
                Url = url,
                Target = target
            };
        }

        private async Task ExecuteProcessorAsync(Processor processor, InstallProfile profile,
            ProcessorContext ctx, string javaPath)
        {
            var cp = new List<string>();
            if (processor.Classpath != null)
                foreach (var c in processor.Classpath)
                    cp.Add(MavenToLocalPath(c, ctx.LibrariesDir));

            var jarPath = MavenToLocalPath(processor.Jar, ctx.LibrariesDir);
            cp.Add(jarPath);

            var mainClass = GetJarMainClass(jarPath)
                ?? throw new IOException($"Main-Class not found in processor JAR: {jarPath}");

            var args = new List<string>
            {
                "-cp", string.Join(Path.PathSeparator, cp), mainClass
            };

            if (processor.Args != null)
                foreach (var arg in processor.Args)
                    args.Add(ResolveArg(arg, profile, ctx));

            var startInfo = new ProcessStartInfo
            {
                FileName = javaPath,
                Arguments = string.Join(" ", args.Select(EscapeArg)),
                WorkingDirectory = ctx.RootDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo)!;
            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new IOException($"Processor failed with exit code {process.ExitCode}: {mainClass}\n{stderr}");
        }

        private string ResolveArg(string arg, InstallProfile profile, ProcessorContext ctx)
        {
            if (arg == "{ROOT}") return ctx.RootDirectory;
            if (arg == "{INSTALLER}") return ctx.InstallerJar;
            if (arg == "{MINECRAFT_JAR}") return ctx.MinecraftJar;

            if (arg.StartsWith('[') && arg.EndsWith(']'))
                return MavenToLocalPath(arg[1..^1], ctx.LibrariesDir);

            if (arg.StartsWith('\'') && arg.EndsWith('\''))
                return arg[1..^1];

            if (arg.StartsWith('{') && arg.EndsWith('}'))
            {
                var key = arg[1..^1];
                if (profile.Data != null && profile.Data.TryGetValue(key, out var entry))
                {
                    var value = entry.Client ?? entry.Server;
                    if (value != null)
                    {
                        if (value.StartsWith('[') && value.EndsWith(']'))
                            return MavenToLocalPath(value[1..^1], ctx.LibrariesDir);
                        return value;
                    }
                }
            }

            return arg;
        }

        private string MavenToLocalPath(string coordinate, string librariesDir)
        {
            var parts = coordinate.Split(':');
            var group = parts[0].Replace('.', '/');
            var artifact = parts[1];
            var version = parts[2].Split('@')[0];
            var ext = parts[2].Contains('@') ? parts[2].Split('@')[1] : "jar";
            var fileName = $"{artifact}-{version}.{ext}";
            return Path.Combine(librariesDir, group, artifact, version, fileName);
        }

        private string? GetJarMainClass(string jarPath)
        {
            using var zip = ZipFile.OpenRead(jarPath);
            var entry = zip.GetEntry("META-INF/MANIFEST.MF");
            if (entry == null) return null;

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("Main-Class:"))
                    return line.Substring("Main-Class:".Length).Trim();
            }
            return null;
        }

        private string EscapeArg(string arg)
        {
            if (arg.Contains(' '))
                return "\"" + arg + "\"";
            return arg;
        }

        // ---- 内部模型 ----

        public class InstallProfile
        {
            public string Minecraft { get; set; } = "";
            public List<Processor>? Processors { get; set; }
            public Dictionary<string, DataEntry>? Data { get; set; }
        }

        public class Processor
        {
            public string Jar { get; set; } = "";
            public List<string>? Classpath { get; set; }
            public List<string>? Args { get; set; }
            public string[]? Sides { get; set; }
        }

        public class DataEntry
        {
            public string? Client { get; set; }
            public string? Server { get; set; }
        }

        public class ProcessorContext
        {
            public string RootDirectory { get; set; } = "";
            public string InstallerJar { get; set; } = "";
            public string LibrariesDir { get; set; } = "";
            public string MinecraftJar { get; set; } = "";
        }
    }
}
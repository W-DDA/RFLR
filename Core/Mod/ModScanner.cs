using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MyLauncher.Core.Mod
{
    public static class ModScanner
    {
        /// <summary>
        /// 扫描 mods 文件夹，解析所有 .jar 的元数据。
        /// </summary>
        public static List<ModInfo> Scan(string modsDir)
        {
            var result = new List<ModInfo>();
            if (!Directory.Exists(modsDir))
                return result;

            foreach (var jar in Directory.GetFiles(modsDir, "*.jar"))
            {
                var info = ParseJar(jar);
                if (info != null)
                    result.Add(info);
            }
            return result;
        }

        private static ModInfo? ParseJar(string jarPath)
        {
            try
            {
                using var zip = ZipFile.OpenRead(jarPath);

                // 先试 Fabric
                var fabricEntry = zip.GetEntry("fabric.mod.json");
                if (fabricEntry != null)
                    return ParseFabric(jarPath, fabricEntry);

                // 再试 Forge
                var forgeEntry = zip.GetEntry("META-INF/mods.toml");
                if (forgeEntry != null)
                    return ParseForge(jarPath, forgeEntry);

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static ModInfo ParseFabric(string jarPath, ZipArchiveEntry entry)
        {
            using var stream = entry.Open();
            var doc = JsonDocument.Parse(stream);
            var root = doc.RootElement;

            var info = new ModInfo
            {
                FileName = Path.GetFileName(jarPath),
                LoaderType = "fabric",
                ModId = root.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                Name = root.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                Version = root.TryGetProperty("version", out var ver) ? ver.GetString() ?? "" : ""
            };

            // depends
            if (root.TryGetProperty("depends", out var depends))
            {
                foreach (var prop in depends.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String)
                        info.Depends[prop.Name] = prop.Value.GetString() ?? "*";
                    else
                        info.Depends[prop.Name] = "*";
                }
            }

            // breaks
            if (root.TryGetProperty("breaks", out var breaks))
            {
                foreach (var prop in breaks.EnumerateObject())
                {
                    info.Breaks[prop.Name] = prop.Value.ValueKind == JsonValueKind.String
                        ? prop.Value.GetString() ?? ""
                        : "";
                }
            }

            return info;
        }

        private static ModInfo ParseForge(string jarPath, ZipArchiveEntry entry)
        {
            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var toml = reader.ReadToEnd();

            var info = new ModInfo
            {
                FileName = Path.GetFileName(jarPath),
                LoaderType = "forge"
            };

            // 提取 [[mods]] 里的 modId、displayName、version
            var modIdMatch = Regex.Match(toml, @"modId\s*=\s*""([^""]+)""");
            if (modIdMatch.Success) info.ModId = modIdMatch.Groups[1].Value;

            var nameMatch = Regex.Match(toml, @"displayName\s*=\s*""([^""]+)""");
            if (nameMatch.Success) info.Name = nameMatch.Groups[1].Value;

            var verMatch = Regex.Match(toml, @"version\s*=\s*""([^""]+)""");
            if (verMatch.Success) info.Version = verMatch.Groups[1].Value;

            // 提取依赖 [[dependencies]]
            var depMatches = Regex.Matches(toml,
                @"\[\[dependencies\.[^\]]+\]\]\s*modId\s*=\s*""([^""]+)""\s*\r?\n\s*mandatory\s*=\s*(true|false)\s*\r?\n\s*versionRange\s*=\s*""([^""]*)""");
            foreach (Match m in depMatches)
            {
                var depId = m.Groups[1].Value;
                var mandatory = m.Groups[2].Value == "true";
                var range = m.Groups[3].Value;
                if (mandatory)
                    info.Depends[depId] = range;
            }

            return info;
        }
    }
}
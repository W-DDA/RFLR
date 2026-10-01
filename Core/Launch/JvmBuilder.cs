using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MyLauncher.Core.Version;

namespace MyLauncher.Core.Launch
{
    public class JvmBuilder
    {
        public List<string> Build(VersionJson v, LaunchProfile p)
        {
            var args = new List<string> { p.JavaBin };

            // 1) 版本 JSON 中的 jvm 参数
            if (v.Arguments?.Jvm != null)
            {
                foreach (var o in v.Arguments.Jvm)
                {
                    if (o is JsonElement je)
                    {
                        if (je.ValueKind == JsonValueKind.String)
                            args.Add(Substitute(je.GetString()!, p));
                        else if (je.ValueKind == JsonValueKind.Object)
                        {
                            var rules = ParseRules(je);
                            if (RuleEvaluator.IsAllowed(rules))
                            {
                                if (je.TryGetProperty("value", out var val))
                                {
                                    if (val.ValueKind == JsonValueKind.String)
                                        args.Add(Substitute(val.GetString()!, p));
                                    else if (val.ValueKind == JsonValueKind.Array)
                                        foreach (var item in val.EnumerateArray())
                                            args.Add(Substitute(item.GetString()!, p));
                                }
                            }
                        }
                    }
                }
            }

            // 2) 内存和系统属性
            args.Add($"-Xmx{p.MaxMemoryMb}M");
            args.Add($"-Xms{p.MinMemoryMb}M");
            args.Add($"-Djava.library.path={p.NativesDir}");
            args.Add("-Dminecraft.launcher.brand=MyLauncher");
            args.Add("-Dminecraft.launcher.version=1.0");
            args.Add("-cp");
            args.Add(p.Classpath);
            args.Add(v.MainClass ?? "net.minecraft.client.main.Main");

            // 3) 游戏参数
            if (v.Arguments?.Game != null)
            {
                foreach (var o in v.Arguments.Game)
                {
                    if (o is JsonElement je)
                    {
                        if (je.ValueKind == JsonValueKind.String)
                            args.Add(Substitute(je.GetString()!, p));
                        else if (je.ValueKind == JsonValueKind.Object)
                        {
                            var rules = ParseRules(je);
                            if (RuleEvaluator.IsAllowed(rules))
                            {
                                if (je.TryGetProperty("value", out var val))
                                {
                                    if (val.ValueKind == JsonValueKind.String)
                                        args.Add(Substitute(val.GetString()!, p));
                                    else if (val.ValueKind == JsonValueKind.Array)
                                        foreach (var item in val.EnumerateArray())
                                            args.Add(Substitute(item.GetString()!, p));
                                }
                            }
                        }
                    }
                }
            }
            else if (!string.IsNullOrEmpty(v.MinecraftArguments))
            {
                foreach (var s in v.MinecraftArguments.Split(' '))
                    args.Add(Substitute(s, p));
            }

            return args;
        }

        private List<Library.Rule>? ParseRules(JsonElement obj)
        {
            if (obj.TryGetProperty("rules", out var rules) && rules.ValueKind == JsonValueKind.Array)
            {
                return JsonSerializer.Deserialize<List<Library.Rule>>(rules.GetRawText());
            }
            return null;
        }

        private string Substitute(string s, LaunchProfile p)
        {
            return s.Replace("${natives_directory}", p.NativesDir)
                    .Replace("${launcher_name}", "MyLauncher")
                    .Replace("${launcher_version}", "1.0")
                    .Replace("${classpath}", p.Classpath)
                    .Replace("${classpath_separator}", Path.PathSeparator.ToString())
                    .Replace("${auth_player_name}", p.Username)
                    .Replace("${version_name}", p.VersionId)
                    .Replace("${game_directory}", p.GameDir)
                    .Replace("${assets_root}", p.AssetsDir)
                    .Replace("${assets_index_name}", p.AssetIndex)
                    .Replace("${auth_uuid}", p.Uuid)
                    .Replace("${auth_access_token}", p.AccessToken)
                    .Replace("${user_type}", "msa")
                    .Replace("${version_type}", "release");
        }
    }
}
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MyLauncher.Core.Launch
{
    public static class CrashAnalyzer
    {
        /// <summary>
        /// 分析崩溃日志，返回人类可读的提示。
        /// </summary>
        public static string Analyze(string gameDir)
        {
            var crashDir = Path.Combine(gameDir, "crash-reports");
            if (!Directory.Exists(crashDir))
                return "未找到崩溃报告，可能是 JVM 启动参数问题或内存不足。";

            var latest = new DirectoryInfo(crashDir)
                .GetFiles("crash-*.txt")
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();

            if (latest == null)
                return "未找到崩溃报告，可能是 JVM 启动参数问题或内存不足。";

            var content = File.ReadAllText(latest.FullName);

            var patterns = new (string Pattern, string Message)[]
            {
                (@"java\.lang\.OutOfMemoryError", "内存不足。请在设置页增大最大内存，或减少同时加载的 Mod 数量。"),
                (@"Caused by: org\.spongepowered\.asm\.mixin", "Mixin 冲突。某个 Mod 与当前加载器或游戏版本不兼容，请检查最近安装的 Mod。"),
                (@"Missing or unsupported mandatory dependencies", "缺少前置 Mod。请检查报错中提到的 Mod 依赖是否已安装。"),
                (@"java\.lang\.NoSuchMethodError", "Mod 版本与游戏版本不匹配。请检查报错中提到的 Mod 是否支持当前游戏版本。"),
                (@"java\.lang\.ClassNotFoundException", "缺少类文件。可能是某个 Mod 的前置库未安装，或 Mod 文件损坏。"),
                (@"java\.lang\.NoClassDefFoundError", "类定义未找到。通常是 Mod 依赖缺失或版本冲突。"),
                (@"java\.lang\.StackOverflowError", "栈溢出。通常是某个 Mod 的递归逻辑有问题。"),
                (@"Pixel format not accelerated", "显卡驱动不支持当前 OpenGL 版本。请更新显卡驱动。"),
                (@"Failed to locate library: vulkan-1\.dll", "缺少 Vulkan 运行库。游戏会自动回退到 OpenGL，不影响运行。"),
                (@"The game crashed whilst", "游戏崩溃。请查看下方详细日志。"),
            };

            foreach (var (pattern, message) in patterns)
            {
                if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
                    return message;
            }

            var causedBy = Regex.Match(content, @"Caused by: (.+?)\n");
            if (causedBy.Success)
                return $"崩溃原因: {causedBy.Groups[1].Value.Trim()}";

            return "游戏崩溃，但未匹配到已知错误模式。请查看 crash-reports 文件夹里的详细日志。";
        }

        /// <summary>
        /// 获取最新的崩溃报告路径。
        /// </summary>
        public static string? GetLatestCrashReport(string gameDir)
        {
            var crashDir = Path.Combine(gameDir, "crash-reports");
            if (!Directory.Exists(crashDir)) return null;

            return new DirectoryInfo(crashDir)
                .GetFiles("crash-*.txt")
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault()?.FullName;
        }
    }
}
using System;
using System.IO;
using System.Text.Json;

namespace MyLauncher.Modules
{
    public static class ModConfig
    {
        private static readonly string ConfigPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        public static string GameDir { get; set; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".minecraft_custom");

        public static void Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("GameDir", out var gd))
                    {
                        var val = gd.GetString();
                        if (!string.IsNullOrEmpty(val))
                            GameDir = val;
                    }
                }
            }
            catch
            {
                // 读失败就用默认值
            }
        }

        public static void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(new { GameDir }, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(ConfigPath, json);
            }
            catch
            {
                // 忽略保存失败
            }
        }
    }
}
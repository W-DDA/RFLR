using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MyLauncher.Core.Version
{
    public class VersionJson
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("mainClass")] public string? MainClass { get; set; }
        [JsonPropertyName("assets")] public string? Assets { get; set; }
        [JsonPropertyName("assetIndex")] public AssetIndexDto? AssetIndex { get; set; }
        [JsonPropertyName("arguments")] public ArgumentsDto? Arguments { get; set; }
        [JsonPropertyName("minecraftArguments")] public string? MinecraftArguments { get; set; }
        [JsonPropertyName("libraries")] public List<Library>? Libraries { get; set; }
        [JsonPropertyName("downloads")] public DownloadsDto? Downloads { get; set; }
        [JsonPropertyName("javaVersion")] public JavaVersionDto? JavaVersion { get; set; }
        [JsonPropertyName("inheritsFrom")] public string? InheritsFrom { get; set; }

        public class AssetIndexDto
        {
            [JsonPropertyName("id")] public string Id { get; set; } = "";
            [JsonPropertyName("url")] public string Url { get; set; } = "";
            [JsonPropertyName("sha1")] public string Sha1 { get; set; } = "";
            [JsonPropertyName("size")] public long Size { get; set; }
            [JsonPropertyName("totalSize")] public long TotalSize { get; set; }
        }

        public class DownloadsDto
        {
            [JsonPropertyName("client")] public ArtifactDto? Client { get; set; }
            [JsonPropertyName("server")] public ArtifactDto? Server { get; set; }
        }

        public class JavaVersionDto
        {
            [JsonPropertyName("component")] public string Component { get; set; } = "";
            [JsonPropertyName("majorVersion")] public int MajorVersion { get; set; }
        }

        public class ArgumentsDto
        {
            [JsonPropertyName("game")] public List<object>? Game { get; set; }
            [JsonPropertyName("jvm")] public List<object>? Jvm { get; set; }
        }

        public class ArtifactDto
        {
            [JsonPropertyName("path")] public string Path { get; set; } = "";
            [JsonPropertyName("url")] public string Url { get; set; } = "";
            [JsonPropertyName("sha1")] public string Sha1 { get; set; } = "";
            [JsonPropertyName("size")] public long Size { get; set; }
        }
    }
}
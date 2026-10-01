using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MyLauncher.Core.Version
{
    public class Library
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("downloads")] public DownloadsDto? Downloads { get; set; }
        [JsonPropertyName("rules")] public List<Rule>? Rules { get; set; }
        [JsonPropertyName("natives")] public NativesDto? Natives { get; set; }
        [JsonPropertyName("extract")] public ExtractDto? Extract { get; set; }

        public class DownloadsDto
        {
            [JsonPropertyName("artifact")] public ArtifactDto? Artifact { get; set; }
            [JsonPropertyName("classifiers")] public Dictionary<string, ArtifactDto>? Classifiers { get; set; }
        }

        public class ArtifactDto
        {
            [JsonPropertyName("path")] public string Path { get; set; } = "";
            [JsonPropertyName("url")] public string Url { get; set; } = "";
            [JsonPropertyName("sha1")] public string Sha1 { get; set; } = "";
            [JsonPropertyName("size")] public long Size { get; set; }
        }

        public class NativesDto
        {
            [JsonPropertyName("natives")] public Dictionary<string, string>? NativesMap { get; set; }
        }

        public class ExtractDto
        {
            [JsonPropertyName("exclude")] public List<string>? Exclude { get; set; }
        }

        public class Rule
        {
            [JsonPropertyName("action")] public string Action { get; set; } = "";
            [JsonPropertyName("os")] public OsDto? Os { get; set; }
            [JsonPropertyName("features")] public Dictionary<string, bool>? Features { get; set; }

            public class OsDto
            {
                [JsonPropertyName("name")] public string? Name { get; set; }
                [JsonPropertyName("arch")] public string? Arch { get; set; }
                [JsonPropertyName("version")] public string? Version { get; set; }
            }
        }

        public string GroupId() => Name.Split(':')[0];
        public string ArtifactId() => Name.Split(':')[1];
        public string Version() => Name.Split(':').Length >= 3 ? Name.Split(':')[2] : "";
        public string GavKey() => $"{GroupId()}:{ArtifactId()}";
    }
}
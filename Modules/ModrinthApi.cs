using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyLauncher.Modules
{
    public class ModSearchResult
    {
        public string ProjectId { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Author { get; set; } = "";
        public long Downloads { get; set; }
        public string IconUrl { get; set; } = "";
    }

    public static class ModrinthApi
    {
        private static readonly HttpClient _http = new HttpClient();
        private const string BaseUrl = "https://api.modrinth.com/v2";

        // 中文映射数据的下载地址（GitHub Release）
        private const string NameMapUrl =
            "https://github.com/W-DDA/RFLR/releases/download/v1.0-data/modname_map.json";

        private static Dictionary<string, string>? _nameMap;

        /// <summary>
        /// 获取中文映射表。优先读本地，本地没有就从 GitHub Release 下载。
        /// </summary>
        private static async Task<Dictionary<string, string>> GetNameMapAsync()
        {
            if (_nameMap != null) return _nameMap;

            _nameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "modname_map.json");

            // 1. 本地已有，直接读
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (dict != null)
                    {
                        foreach (var kv in dict) _nameMap[kv.Key] = kv.Value;
                    }
                    Console.WriteLine($"[Modrinth] 本地映射表加载成功，共 {_nameMap.Count} 条");
                    return _nameMap;
                }
                catch
                {
                    // 本地读取失败，继续尝试下载
                }
            }

            // 2. 本地没有或读取失败，从 GitHub Release 下载
            try
            {
                Console.WriteLine("[Modrinth] 本地无数据，正在从服务器下载中文映射表...");
                var bytes = await _http.GetByteArrayAsync(NameMapUrl);
                await File.WriteAllBytesAsync(path, bytes);

                var json = File.ReadAllText(path);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict != null)
                {
                    foreach (var kv in dict) _nameMap[kv.Key] = kv.Value;
                }
                Console.WriteLine($"[Modrinth] 下载完成，加载 {_nameMap.Count} 条映射");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Modrinth] 下载失败: {e.Message}");
            }

            return _nameMap;
        }

        /// <summary>
        /// 中文关键词翻译成英文。
        /// </summary>
        private static async Task<string> TranslateQueryAsync(string query)
        {
            if (!Regex.IsMatch(query, @"[\u4e00-\u9fff]"))
            {
                Console.WriteLine($"[Modrinth] 非中文关键词，不翻译: {query}");
                return query;
            }

            var map = await GetNameMapAsync();

            if (map.TryGetValue(query.Trim(), out var english))
            {
                Console.WriteLine($"[Modrinth] 精确匹配: {query} -> {english}");
                return english;
            }

            foreach (var kv in map)
            {
                if (query.Contains(kv.Key))
                {
                    Console.WriteLine($"[Modrinth] 模糊匹配: {query} -> {kv.Value}");
                    return kv.Value;
                }
            }

            Console.WriteLine($"[Modrinth] 映射表里没有: {query}");
            return query;
        }

        public static async Task<List<ModSearchResult>> SearchAsync(
            string query, string? gameVersion = null, string? loader = null)
        {
            var searchQuery = await TranslateQueryAsync(query);

            var facets = new List<string> { "[\"project_type:mod\"]" };
            if (!string.IsNullOrEmpty(gameVersion))
                facets.Add($"[\"versions:{gameVersion}\"]");
            if (!string.IsNullOrEmpty(loader))
                facets.Add($"[\"categories:{loader}\"]");

            // 第一次：用原始关键词搜
            var results = await DoSearchAsync(searchQuery, facets);
            Console.WriteLine($"[Modrinth] 第一次搜索 '{searchQuery}' 返回 {results.Count} 个结果");

            // 如果没结果，去掉末尾数字再搜一次
            if (results.Count == 0)
            {
                var cleaned = Regex.Replace(searchQuery, @"\s+\d+$", "");
                if (cleaned != searchQuery)
                {
                    Console.WriteLine($"[Modrinth] 第一次无结果，去掉数字后缀再搜: {cleaned}");
                    results = await DoSearchAsync(cleaned, facets);
                    Console.WriteLine($"[Modrinth] 第二次搜索 '{cleaned}' 返回 {results.Count} 个结果");
                }
            }

            return results;
        }

        private static async Task<List<ModSearchResult>> DoSearchAsync(string query, List<string> facets)
        {
            var url = $"{BaseUrl}/search?query={Uri.EscapeDataString(query)}" +
                      $"&facets=[{string.Join(",", facets)}]&limit=20";

            Console.WriteLine($"[Modrinth] 搜索URL: {url}");

            var response = await _http.GetStringAsync(url);
            var doc = JsonDocument.Parse(response);
            var hits = doc.RootElement.GetProperty("hits");

            var results = new List<ModSearchResult>();
            foreach (var hit in hits.EnumerateArray())
            {
                results.Add(new ModSearchResult
                {
                    ProjectId = hit.GetProperty("project_id").GetString() ?? "",
                    Title = hit.GetProperty("title").GetString() ?? "",
                    Description = hit.GetProperty("description").GetString() ?? "",
                    Author = hit.GetProperty("author").GetString() ?? "",
                    Downloads = hit.GetProperty("downloads").GetInt64(),
                    IconUrl = hit.TryGetProperty("icon_url", out var icon)
                        ? icon.GetString() ?? ""
                        : ""
                });
            }
            return results;
        }

        public static async Task<(string? url, string? fileName)> GetDownloadAsync(
            string projectId, string gameVersion, string loader)
        {
            var url = $"{BaseUrl}/project/{projectId}/version" +
                      $"?game_versions=[\"{gameVersion}\"]&loaders=[\"{loader}\"]";

            var response = await _http.GetStringAsync(url);
            var doc = JsonDocument.Parse(response);
            var versions = doc.RootElement;

            if (versions.GetArrayLength() == 0)
                return (null, null);

            var first = versions[0];
            var files = first.GetProperty("files");
            if (files.GetArrayLength() == 0)
                return (null, null);

            var file = files[0];
            var downloadUrl = file.GetProperty("url").GetString();
            var fileName = file.GetProperty("filename").GetString();
            return (downloadUrl, fileName);
        }

        public static async Task DownloadFileAsync(string url, string destPath)
        {
            var bytes = await _http.GetByteArrayAsync(url);
            var dir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            await File.WriteAllBytesAsync(destPath, bytes);
        }
    }
}
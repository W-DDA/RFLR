using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MyLauncher.Core.Download;
using MyLauncher.Core.Util;

namespace MyLauncher.Core.Version
{
    public class VersionManager
    {
        private readonly string _root;
        private readonly DownloadManager _downloadManager;

        public VersionManager(string root, int threads)
        {
            _root = root;
            _downloadManager = new DownloadManager(threads);
        }

        /// <summary>
        /// 解析版本（含继承链合并）。
        /// </summary>
        public VersionJson Resolve(string versionId)
        {
            var jsonPath = Path.Combine(_root, "versions", versionId, versionId + ".json");
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"Version not found: {versionId}");

            var child = JsonUtil.Read<VersionJson>(jsonPath);
            if (string.IsNullOrEmpty(child.InheritsFrom))
                return child;

            var parent = Resolve(child.InheritsFrom);
            return Merge(parent, child);
        }

        private VersionJson Merge(VersionJson parent, VersionJson child)
        {
            var v = new VersionJson
            {
                Id = child.Id,
                MainClass = child.MainClass ?? parent.MainClass,
                Assets = child.Assets ?? parent.Assets,
                AssetIndex = child.AssetIndex ?? parent.AssetIndex,
                Downloads = child.Downloads ?? parent.Downloads,
                JavaVersion = child.JavaVersion ?? parent.JavaVersion,
                MinecraftArguments = child.MinecraftArguments ?? parent.MinecraftArguments,
                Arguments = MergeArgs(parent.Arguments, child.Arguments)
            };

            // 库合并：子版本同 GA 覆盖父版本
            var map = new Dictionary<string, Library>();
            if (parent.Libraries != null)
                foreach (var l in parent.Libraries) map[l.GavKey()] = l;
            if (child.Libraries != null)
                foreach (var l in child.Libraries) map[l.GavKey()] = l;
            v.Libraries = map.Values.ToList();

            return v;
        }

        private VersionJson.ArgumentsDto? MergeArgs(VersionJson.ArgumentsDto? parent, VersionJson.ArgumentsDto? child)
        {
            if (parent == null && child == null) return null;
            var merged = new VersionJson.ArgumentsDto
            {
                Jvm = new List<object>(),
                Game = new List<object>()
            };
            if (parent?.Jvm != null) merged.Jvm.AddRange(parent.Jvm);
            if (parent?.Game != null) merged.Game.AddRange(parent.Game);
            if (child?.Jvm != null) merged.Jvm.AddRange(child.Jvm);
            if (child?.Game != null) merged.Game.AddRange(child.Game);
            return merged;
        }

        /// <summary>
        /// 构建所有下载任务。
        /// </summary>
        public List<DownloadTask> BuildTasks(VersionJson v)
        {
            var tasks = new List<DownloadTask>();

            // 1) client.jar
            if (v.Downloads?.Client != null)
            {
                tasks.Add(new DownloadTask
                {
                    Url = v.Downloads.Client.Url,
                    Sha1 = v.Downloads.Client.Sha1,
                    Size = v.Downloads.Client.Size,
                    Target = Path.Combine(_root, "versions", v.Id, v.Id + ".jar")
                });
            }

            // 2) libraries
            if (v.Libraries != null)
            {
                foreach (var lib in v.Libraries)
                {
                    if (!RuleEvaluator.IsAllowed(lib.Rules)) continue;

                    if (lib.Downloads?.Artifact != null)
                    {
                        tasks.Add(new DownloadTask
                        {
                            Url = lib.Downloads.Artifact.Url,
                            Sha1 = lib.Downloads.Artifact.Sha1,
                            Size = lib.Downloads.Artifact.Size,
                            Target = Path.Combine(_root, "libraries", lib.Downloads.Artifact.Path)
                        });
                    }

                    // natives
                    if (lib.Natives?.NativesMap != null && lib.Downloads?.Classifiers != null)
                    {
                        var osName = RuleEvaluator.OsName();
                        if (lib.Natives.NativesMap.TryGetValue(osName, out var key))
                        {
                            var classifier = key.Replace("${arch}", "64");
                            if (lib.Downloads.Classifiers.TryGetValue(classifier, out var nativeArt))
                            {
                                tasks.Add(new DownloadTask
                                {
                                    Url = nativeArt.Url,
                                    Sha1 = nativeArt.Sha1,
                                    Size = nativeArt.Size,
                                    Target = Path.Combine(_root, "libraries", nativeArt.Path)
                                });
                            }
                        }
                    }
                }
            }

            return tasks;
        }

        public async System.Threading.Tasks.Task DownloadAllAsync(List<DownloadTask> tasks)
        {
            await _downloadManager.SubmitAsync(tasks);
        }
    }
}
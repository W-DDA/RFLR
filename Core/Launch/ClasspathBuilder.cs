using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MyLauncher.Core.Version;

namespace MyLauncher.Core.Launch
{
    public class ClasspathBuilder
    {
        /// <summary>
        /// 硬化的 Classpath 构建：版本冲突裁决。
        /// 同一 GA 多个版本时选最高版本。
        /// </summary>
        public string Build(VersionJson v, string root, string versionDir)
        {
            var byGav = new Dictionary<string, Library>();

            if (v.Libraries != null)
            {
                foreach (var lib in v.Libraries)
                {
                    if (!RuleEvaluator.IsAllowed(lib.Rules)) continue;
                    if (lib.Downloads?.Artifact == null) continue;

                    var key = lib.GavKey();
                    if (!byGav.TryGetValue(key, out var existing))
                    {
                        byGav[key] = lib;
                    }
                    else if (CompareVersions(lib.Version(), existing.Version()) > 0)
                    {
                        byGav[key] = lib;
                    }
                }
            }

            var cp = new List<string>
            {
                Path.Combine(versionDir, v.Id + ".jar")
            };

            foreach (var lib in byGav.Values)
            {
                var p = Path.Combine(root, "libraries", lib.Downloads!.Artifact!.Path);
                if (File.Exists(p))
                    cp.Add(p);
            }

            return string.Join(Path.PathSeparator, cp);
        }

        private int CompareVersions(string a, string b)
        {
            var pa = a.Split('.', '-');
            var pb = b.Split('.', '-');
            int len = Math.Max(pa.Length, pb.Length);
            for (int i = 0; i < len; i++)
            {
                int va = i < pa.Length ? ParseIntSafe(pa[i]) : 0;
                int vb = i < pb.Length ? ParseIntSafe(pb[i]) : 0;
                if (va != vb) return va.CompareTo(vb);
            }
            return 0;
        }

        private int ParseIntSafe(string s)
        {
            var digits = new string(s.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var n) ? n : 0;
        }
    }
}
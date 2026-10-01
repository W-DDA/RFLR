using System;
using System.Collections.Generic;

namespace MyLauncher.Core.Version
{
    public static class RuleEvaluator
    {
        public static bool IsAllowed(List<Library.Rule>? rules)
        {
            if (rules == null || rules.Count == 0) return true;

            bool allowed = false;
            foreach (var rule in rules)
            {
                if (Matches(rule))
                    allowed = rule.Action == "allow";
            }
            return allowed;
        }

        private static bool Matches(Library.Rule rule)
        {
            if (rule.Os != null)
            {
                var name = OsName();
                if (rule.Os.Name != null && rule.Os.Name != name) return false;
                if (rule.Os.Arch != null && rule.Os.Arch != Arch()) return false;
            }
            if (rule.Features != null)
            {
                foreach (var kv in rule.Features)
                {
                    if (FeatureFlags.Get(kv.Key) != kv.Value) return false;
                }
            }
            return true;
        }

        public static string OsName()
        {
            if (OperatingSystem.IsWindows()) return "windows";
            if (OperatingSystem.IsMacOS()) return "osx";
            return "linux";
        }

        public static string Arch()
        {
            return System.Runtime.InteropServices.RuntimeInformation.OSArchitecture
                == System.Runtime.InteropServices.Architecture.X64 ? "x86_64" : "x86";
        }

        public static class FeatureFlags
        {
            public static bool Get(string key) => false;
        }
    }
}
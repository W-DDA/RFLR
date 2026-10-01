using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MyLauncher.Core.Version;

namespace MyLauncher.Core.Launch
{
    public class GameLauncher
    {
        public (Process Process, GameProcessMonitor Monitor) Launch(
            VersionJson v, LaunchProfile p, Action<string>? onLog = null)
        {
            var cmd = new JvmBuilder().Build(v, p);

            var startInfo = new ProcessStartInfo
            {
                FileName = cmd[0],
                Arguments = string.Join(" ", cmd.Skip(1).Select(EscapeArg)),
                WorkingDirectory = p.GameDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };

            var process = Process.Start(startInfo)!;
            process.EnableRaisingEvents = true;   // 关键：进程创建后立刻设置

            var monitor = new GameProcessMonitor();
            monitor.Monitor(process, onLog);
            return (process, monitor);
        }

        private string EscapeArg(string arg)
        {
            if (arg.Contains(' '))
                return "\"" + arg + "\"";
            return arg;
        }
    }
}
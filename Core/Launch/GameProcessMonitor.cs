using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MyLauncher.Core.Launch
{
    public class GameProcessMonitor
    {
        private readonly StringBuilder _rawOutput = new();

        public void Monitor(Process process, Action<string>? onLog = null)
        {
            _ = Task.Run(() => ReadStream(process.StandardOutput, false, onLog));
            _ = Task.Run(() => ReadStream(process.StandardError, true, onLog));
        }

        private void ReadStream(StreamReader reader, bool isError, Action<string>? onLog)
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                lock (_rawOutput)
                {
                    _rawOutput.AppendLine(line);
                }
                onLog?.Invoke(isError ? "[ERR] " + line : line);
            }
        }

        public string GetRawOutput()
        {
            lock (_rawOutput)
            {
                return _rawOutput.ToString();
            }
        }
    }
}
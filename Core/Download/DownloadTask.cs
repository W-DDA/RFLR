using System;
using System.IO;
using System.Threading.Tasks;

namespace MyLauncher.Core.Download
{
    public class DownloadTask
    {
        public string Url { get; set; } = "";
        public string? FallbackUrl { get; set; }
        public string Target { get; set; } = "";
        public string? Sha1 { get; set; }
        public long Size { get; set; }

        private readonly RangeAwareDownloader _downloader = new();

        public async Task RunAsync(Action<long, long>? listener)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Target)!);

            if (!string.IsNullOrEmpty(Sha1) && File.Exists(Target)
                && Sha1.Equals(HashUtil.Hash(Target, "SHA-1"), StringComparison.OrdinalIgnoreCase))
            {
                listener?.Invoke(Size, Size);
                return;
            }

            try
            {
                await _downloader.DownloadAsync(Url, Target, listener);
            }
            catch (IOException) when (!string.IsNullOrEmpty(FallbackUrl))
            {
                await _downloader.DownloadAsync(FallbackUrl!, Target, listener);
            }

            if (!string.IsNullOrEmpty(Sha1))
            {
                var actual = HashUtil.Hash(Target, "SHA-1");
                if (!Sha1.Equals(actual, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(Target);
                    throw new IOException($"SHA1 mismatch: expected={Sha1} actual={actual}");
                }
            }
        }
    }
}
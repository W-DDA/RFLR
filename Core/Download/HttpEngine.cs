using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MyLauncher.Core.Download
{
    public class HttpEngine
    {
        protected readonly HttpClient Client;
        protected readonly int ConnectTimeout = 10000;
        protected readonly int ReadTimeout = 30000;

        public HttpEngine()
        {
            Client = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 5,
                MaxConnectionsPerServer = 16
            })
            {
                Timeout = TimeSpan.FromMilliseconds(ReadTimeout)
            };
            Client.DefaultRequestHeaders.Add("User-Agent", "MinecraftLauncher/1.0");
        }

        public async Task DownloadToFileAsync(string url, string target,
            long startOffset, Action<long, long>? listener, RetryPolicy retry)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    await DoDownloadAsync(url, target, startOffset, listener);
                    return;
                }
                catch (Exception) when (++attempt <= retry.MaxAttempts)
                {
                    await Task.Delay((int)retry.BackoffMillis(attempt));
                }
            }
        }

        private async Task DoDownloadAsync(string url, string target,
            long startOffset, Action<long, long>? listener)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (startOffset > 0)
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(startOffset, null);

            using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            long actualStart = 0;
            if (response.StatusCode == System.Net.HttpStatusCode.PartialContent)
                actualStart = startOffset;
            else if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new IOException($"HTTP {(int)response.StatusCode} -> {url}");

            var dir = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            using var input = await response.Content.ReadAsStreamAsync();
            using var raf = new FileStream(target, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            raf.Seek(actualStart, SeekOrigin.Begin);

            var buffer = new byte[8192];
            long written = actualStart;
            int n;
            while ((n = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await raf.WriteAsync(buffer, 0, n);
                written += n;
                listener?.Invoke(written, -1);
            }
        }
    }

    public class RetryPolicy
    {
        private readonly int _maxAttempts;
        private readonly long _baseDelay;
        private static readonly Random Rng = new();

        public RetryPolicy(int maxAttempts, long baseDelay)
        {
            _maxAttempts = maxAttempts;
            _baseDelay = baseDelay;
        }

        public static RetryPolicy Defaults => new(3, 1000);
        public static RetryPolicy Quick => new(1, 200);
        public int MaxAttempts => _maxAttempts;

        public long BackoffMillis(int attempt)
        {
            long delay = _baseDelay * (1L << (attempt - 1));
            return delay + Rng.NextInt64(delay / 2 + 1);
        }
    }
}
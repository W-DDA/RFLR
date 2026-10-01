using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MyLauncher.Core.Download
{
    public class RangeAwareDownloader : HttpEngine
    {
        public async Task DownloadAsync(string url, string target, Action<long, long>? listener)
        {
            var temp = target + ".part";
            long existing = File.Exists(temp) ? new FileInfo(temp).Length : 0;

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (existing > 0)
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existing, null);

            using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            var code = (int)response.StatusCode;

            if (code == 200 && existing > 0)
            {
                File.Delete(temp);
                existing = 0;
            }
            else if (code != 206 && code != 200)
            {
                throw new IOException($"HTTP {code} -> {url}");
            }

            long total = code == 206
                ? existing + (response.Content.Headers.ContentLength ?? 0)
                : (response.Content.Headers.ContentLength ?? 0);

            using (var input = await response.Content.ReadAsStreamAsync())
            using (var raf = new FileStream(temp, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
            {
                raf.Seek(existing, SeekOrigin.Begin);
                var buffer = new byte[8192];
                long written = existing;
                int n;
                while ((n = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await raf.WriteAsync(buffer, 0, n);
                    written += n;
                    listener?.Invoke(written, total);
                }
            }

            File.Move(temp, target, true);
        }
    }
}
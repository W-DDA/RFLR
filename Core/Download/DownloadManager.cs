using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyLauncher.Core.Download
{
    public class DownloadManager
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly List<Exception> _errors = new();
        private long _totalBytes;
        private long _doneBytes;
        private volatile bool _cancelled;

        public DownloadManager(int threads)
        {
            _semaphore = new SemaphoreSlim(Math.Clamp(threads, 1, 16));
        }

        public async Task SubmitAsync(List<DownloadTask> tasks)
        {
            foreach (var t in tasks)
                Interlocked.Add(ref _totalBytes, Math.Max(t.Size, 0));

            var all = tasks.Select(async task =>
            {
                await _semaphore.WaitAsync();
                try
                {
                    if (_cancelled) return;
                    await task.RunAsync((cur, total) =>
                    {
                        Interlocked.Add(ref _doneBytes, cur);
                    });
                }
                catch (Exception e)
                {
                    lock (_errors) _errors.Add(e);
                }
                finally
                {
                    _semaphore.Release();
                }
            });

            await Task.WhenAll(all);

            if (_errors.Count > 0)
                throw new AggregateException($"下载失败 {_errors.Count} 个文件", _errors);
        }

        public void Cancel() => _cancelled = true;

        public double Progress()
        {
            long total = Interlocked.Read(ref _totalBytes);
            return total == 0 ? 0 : (double)Interlocked.Read(ref _doneBytes) / total;
        }
    }
}
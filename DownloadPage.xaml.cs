using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using MyLauncher.Core.Download;
using MyLauncher.Core.Version;
using MyLauncher.Modules;

namespace MyLauncher
{
    public partial class DownloadPage : System.Windows.Controls.UserControl
    {
        private CancellationTokenSource? _cts;

        public DownloadPage()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadRemoteVersions();
        }

        private async void LoadRemoteVersions()
        {
            try
            {
                Log("正在从服务器获取版本列表...");
                using var http = new HttpClient();
                var json = await http.GetStringAsync(
                    "https://bmclapi2.bangbang93.com/mc/game/version_manifest.json");

                var doc = System.Text.Json.JsonDocument.Parse(json);
                var versions = doc.RootElement.GetProperty("versions");

                var releases = new List<string>();
                var snapshots = new List<string>();
                var oldVersions = new List<string>();

                foreach (var v in versions.EnumerateArray())
                {
                    var id = v.GetProperty("id").GetString()!;
                    var type = v.GetProperty("type").GetString()!;

                    if (type == "release") releases.Add(id);
                    else if (type == "snapshot") snapshots.Add(id);
                    else oldVersions.Add(id);
                }

                ReleaseList.ItemsSource = releases;
                SnapshotList.ItemsSource = snapshots;
                OldAlphaList.ItemsSource = oldVersions;

                Log($"正式版 {releases.Count} 个，预览版 {snapshots.Count} 个，远古版 {oldVersions.Count} 个");
            }
            catch (Exception ex)
            {
                Log($"获取版本列表失败: {ex.Message}");
            }
        }

        private void RefreshRemote_Click(object sender, RoutedEventArgs e)
        {
            LoadRemoteVersions();
        }

        private void VersionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not System.Windows.Controls.ListBox current) return;
            if (current.SelectedItem == null) return;

            foreach (var lb in new[] { ReleaseList, SnapshotList, OldAlphaList })
            {
                if (lb != current && lb.SelectedItem != null)
                    lb.SelectedItem = null;
            }
        }

        private string? GetSelectedVersion()
        {
            if (ReleaseList.SelectedItem is string r) return r;
            if (SnapshotList.SelectedItem is string s) return s;
            if (OldAlphaList.SelectedItem is string o) return o;
            return null;
        }

        private void UpdateGlobalProgress(double percent, string text)
        {
            if (Window.GetWindow(this) is MainWindow main)
            {
                main.ShowGlobalProgress(text);
                main.UpdateGlobalProgress(percent, text);
            }
        }

        private void HideGlobalProgress()
        {
            if (Window.GetWindow(this) is MainWindow main)
                main.HideGlobalProgress();
        }

        private async void Install_Click(object sender, RoutedEventArgs e)
        {
            var version = GetSelectedVersion();
            if (string.IsNullOrEmpty(version))
            {
                Log("请先选择一个版本");
                return;
            }

            if (ModMinecraft.IsVersionInstalled(ModConfig.GameDir, version))
            {
                var result = System.Windows.MessageBox.Show(
                    $"版本 {version} 似乎已安装，是否重新下载？",
                    "确认", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes) return;
            }

            InstallBtn.IsEnabled = false;
            Log($"开始下载 {version}...");
            UpdateGlobalProgress(0, $"开始下载 {version}...");

            _cts = new CancellationTokenSource();

            try
            {
                var root = ModConfig.GameDir;
                var versionDir = Path.Combine(root, "versions", version);
                Directory.CreateDirectory(versionDir);

                var jsonUrl = $"https://bmclapi2.bangbang93.com/version/{version}/json";
                using var http = new HttpClient();
                var json = await http.GetStringAsync(jsonUrl);
                var jsonPath = Path.Combine(versionDir, version + ".json");
                await File.WriteAllTextAsync(jsonPath, json);

                var vm = new VersionManager(root, 8);
                var versionJson = vm.Resolve(version);
                var tasks = vm.BuildTasks(versionJson);

                Log($"需要下载 {tasks.Count} 个文件");
                UpdateGlobalProgress(5, $"共 {tasks.Count} 个文件，开始下载...");

                var dm = new DownloadManager(8);
                await dm.SubmitAsync(tasks);

                UpdateGlobalProgress(100, "安装完成");
                Log($"{version} 安装完成");
                System.Windows.MessageBox.Show($"版本 {version} 安装成功！", "完成");
            }
            catch (OperationCanceledException)
            {
                Log("下载已取消");
            }
            catch (Exception ex)
            {
                Log($"下载失败: {ex.Message}");
            }
            finally
            {
                InstallBtn.IsEnabled = true;
                HideGlobalProgress();
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void Log(string msg)
        {
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg}");
        }
    }
}
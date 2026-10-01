using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MyLauncher.Core.Launch;
using MyLauncher.Core.Version;
using MyLauncher.Modules;

namespace MyLauncher
{
    public partial class GamePage : System.Windows.Controls.UserControl
    {
        private Process? _gameProcess;
        private GameProcessMonitor? _monitor;

        public GamePage()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadVersions();
        }

        private void LoadVersions()
        {
            try
            {
                Log("正在读取本地已安装版本...");
                var list = ModMinecraft.GetLocalVersions(ModConfig.GameDir);

                VersionCombo.ItemsSource = list;
                if (list.Count > 0)
                    VersionCombo.SelectedIndex = 0;

                Log($"已找到 {list.Count} 个本地版本");
            }
            catch (Exception ex)
            {
                Log($"读取版本失败: {ex.Message}");
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadVersions();
        }

        private void Launch_Click(object sender, RoutedEventArgs e)
        {
            if (VersionCombo.SelectedItem is not string version || string.IsNullOrEmpty(version))
            {
                Log("请先选择一个版本");
                return;
            }

            string username = UsernameBox.Text.Trim();
            if (string.IsNullOrEmpty(username))
                username = "Steve";

            LaunchBtn.IsEnabled = false;
            Log($"准备启动 {version}，用户名 {username}");

            try
            {
                var root = ModConfig.GameDir;
                var versionDir = Path.Combine(root, "versions", version);

                var instanceDir = Path.Combine(versionDir, "instance");
                Directory.CreateDirectory(instanceDir);

                var vm = new VersionManager(root, 8);
                var versionJson = vm.Resolve(version);

                var classpath = new ClasspathBuilder().Build(versionJson, root, versionDir);
                var nativesDir = Path.Combine(versionDir, "natives");
                Directory.CreateDirectory(nativesDir);

                var profile = new LaunchProfile
                {
                    JavaBin = FindJavaPath(),
                    VersionId = version,
                    GameDir = instanceDir,
                    AssetsDir = Path.Combine(root, "assets"),
                    AssetIndex = versionJson.AssetIndex?.Id ?? "legacy",
                    Classpath = classpath,
                    NativesDir = nativesDir,
                    MaxMemoryMb = 4096,
                    MinMemoryMb = 1024,
                    Username = username,
                    Uuid = "00000000-0000-0000-0000-000000000000",
                    AccessToken = "0"
                };

                var launcher = new GameLauncher();
                (_gameProcess, _monitor) = launcher.Launch(versionJson, profile, line =>
                {
                    Dispatcher.Invoke(() => Log(line));
                });

                // 只在崩溃时弹窗，正常退出不弹
                _gameProcess.Exited += (s, args) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (_gameProcess.ExitCode != 0)
                        {
                            var analysis = CrashAnalyzer.Analyze(instanceDir);
                            var reportPath = CrashAnalyzer.GetLatestCrashReport(instanceDir);
                            var detail = reportPath != null
                                ? File.ReadAllText(reportPath)
                                : "未找到详细日志";

                            var owner = Window.GetWindow(this);
                            var window = new CrashReportWindow("游戏崩溃: " + analysis, detail);
                            if (owner != null) window.Owner = owner;
                            window.ShowDialog();
                        }
                    });
                };

                Log($"游戏已启动，实例目录: {instanceDir}");
            }
            catch (Exception ex)
            {
                Log($"启动失败: {ex.Message}");
            }
            finally
            {
                LaunchBtn.IsEnabled = true;
            }
        }

        private void OpenInstanceDir_Click(object sender, RoutedEventArgs e)
        {
            if (VersionCombo.SelectedItem is not string version || string.IsNullOrEmpty(version))
            {
                Log("请先选择一个版本");
                return;
            }

            var instanceDir = Path.Combine(ModConfig.GameDir, "versions", version, "instance");
            Directory.CreateDirectory(instanceDir);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = instanceDir,
                UseShellExecute = true
            });
        }

        private string FindJavaPath()
        {
            var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrEmpty(javaHome))
            {
                var p = Path.Combine(javaHome, "bin", "java.exe");
                if (File.Exists(p)) return p;
            }

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var javaDir = Path.Combine(programFiles, "Java");
            if (Directory.Exists(javaDir))
            {
                var candidates = Directory.GetDirectories(javaDir)
                    .OrderByDescending(d => d)
                    .Select(d => Path.Combine(d, "bin", "java.exe"))
                    .Where(File.Exists);

                var found = candidates.FirstOrDefault();
                if (found != null) return found;
            }

            return "java";
        }

        private void Log(string msg)
        {
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg}");
        }
    }
}
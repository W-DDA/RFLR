using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using MyLauncher.Core.Mod;
using MyLauncher.Core.Version;
using MyLauncher.Modules;

namespace MyLauncher
{
    public partial class ModPage : System.Windows.Controls.UserControl
    {
        public ModPage()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                LoadLocalVersions();
                TestScan();
            };
        }

        private void LoadLocalVersions()
        {
            var versions = ModMinecraft.GetLocalVersions(ModConfig.GameDir);
            GameVersionCombo.ItemsSource = versions;
            if (versions.Count > 0)
                GameVersionCombo.SelectedIndex = 0;
        }

        /// <summary>
        /// 临时测试：扫描已安装的 Mod。
        /// </summary>
        private void TestScan()
        {
            // 改成你实际装过 Mod 的实例目录
            var testVersion = "1.20.1-forge-47.2.0";
            var modsDir = Path.Combine(ModConfig.GameDir, "versions", testVersion, "instance", "mods");

            Console.WriteLine($"[ModScanner] 扫描目录: {modsDir}");
            var mods = ModScanner.Scan(modsDir);
            Console.WriteLine($"[ModScanner] 扫描到 {mods.Count} 个 Mod");

            foreach (var m in mods)
            {
                Console.WriteLine($"  {m.FileName}");
                Console.WriteLine($"    id={m.ModId}, name={m.Name}, version={m.Version}, loader={m.LoaderType}");
                Console.WriteLine($"    depends={m.Depends.Count}, breaks={m.Breaks.Count}");
            }
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            var query = SearchBox.Text.Trim();
            if (string.IsNullOrEmpty(query)) return;

            try
            {
                var results = await ModrinthApi.SearchAsync(query);
                ResultList.ItemsSource = results;
                Console.WriteLine($"搜索到 {results.Count} 个结果");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"搜索失败: {ex.Message}");
            }
        }

        private async void Install_Click(object sender, RoutedEventArgs e)
        {
            if (ResultList.SelectedItem is not ModSearchResult mod)
            {
                Console.WriteLine("请先在列表里选中一个 Mod");
                return;
            }
            if (GameVersionCombo.SelectedItem is not string version || string.IsNullOrEmpty(version))
            {
                Console.WriteLine("请先选择目标游戏版本");
                return;
            }
            var loader = (LoaderCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "fabric";

            try
            {
                Console.WriteLine($"正在查找 {mod.Title} 对应的 {version} / {loader} 版本...");
                var (url, fileName) = await ModrinthApi.GetDownloadAsync(mod.ProjectId, version, loader);

                if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(fileName))
                {
                    Console.WriteLine($"没有找到匹配 {version} / {loader} 的版本");
                    return;
                }

                var instanceDir = Path.Combine(ModConfig.GameDir, "versions", version, "instance");
                var modsDir = Path.Combine(instanceDir, "mods");
                Directory.CreateDirectory(modsDir);

                var destPath = Path.Combine(modsDir, fileName);

                Console.WriteLine($"正在下载 {fileName}...");
                await ModrinthApi.DownloadFileAsync(url, destPath);
                Console.WriteLine($"已安装到: {destPath}");
                System.Windows.MessageBox.Show($"Mod 已安装到 {modsDir}", "完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"安装失败: {ex.Message}");
            }
        }

        private async void InstallLoader_Click(object sender, RoutedEventArgs e)
        {
            if (GameVersionCombo.SelectedItem is not string version || string.IsNullOrEmpty(version))
            {
                Console.WriteLine("请先选择目标游戏版本");
                return;
            }
            var loader = (LoaderCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "fabric";

            try
            {
                Console.WriteLine($"正在安装 {loader} 到 {version}...");
                var installer = new LoaderInstaller(ModConfig.GameDir);

                if (loader == "fabric")
                {
                    var id = await installer.InstallFabricAsync(version, "0.15.11");
                    Console.WriteLine($"Fabric 安装完成，版本名: {id}");
                    System.Windows.MessageBox.Show($"Fabric 已安装，版本名: {id}", "完成");
                }
                else
                {
                    var id = await installer.InstallForgeAsync(version, "47.2.0", "java");
                    Console.WriteLine($"Forge 安装完成，版本名: {id}");
                    System.Windows.MessageBox.Show($"Forge 已安装，版本名: {id}", "完成");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"安装失败: {ex.Message}");
            }
        }
    }
}
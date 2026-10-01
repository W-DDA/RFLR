using System;
using System.Windows;
using System.Windows.Controls;
using MyLauncher.Modules;

using WinForms = System.Windows.Forms;

namespace MyLauncher
{
    public partial class SettingsPage : System.Windows.Controls.UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();
            GameDirBox.Text = ModConfig.GameDir;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new WinForms.FolderBrowserDialog
            {
                Description = "选择游戏目录",
                SelectedPath = ModConfig.GameDir,
                ShowNewFolderButton = true
            };

            if (folderDialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                GameDirBox.Text = folderDialog.SelectedPath;
                Log($"已选择: {folderDialog.SelectedPath}");
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ModConfig.GameDir = GameDirBox.Text;
            ModConfig.Save();
            Log($"已保存游戏目录: {ModConfig.GameDir}");
            System.Windows.MessageBox.Show("保存成功，重启启动器后生效。", "提示");
        }

        private void Log(string msg)
        {
            LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
            LogBox.ScrollToEnd();
        }
    }
}
using System;
using System.Windows;
using MyLauncher.Modules;

namespace MyLauncher
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 连接数限制由 HttpClientHandler.MaxConnectionsPerServer 控制，
            // 不再使用已过时的 ServicePointManager。

            ModConfig.Load();

            var splash = new SplashWindow();
            splash.Show();

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2.4)
            };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                var main = new MainWindow();
                main.Show();
                splash.Close();
            };
            timer.Start();
        }
    }
}
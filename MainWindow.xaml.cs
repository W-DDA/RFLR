using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MyLauncher
{
    public partial class MainWindow : Window
    {
        private const double NavItemHeight = 42;

        public MainWindow()
        {
            InitializeComponent();

            NavGame.IsChecked = true;
            ShowPage(0);
            MoveIndicator(0, animate: false);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void Nav_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.RadioButton btn) return;
            if (btn.Tag is not string tag) return;

            int index = int.Parse(tag);
            ShowPage(index);
            MoveIndicator(index, animate: true);
        }

        private void MoveIndicator(int index, bool animate)
        {
            double targetY = index * NavItemHeight;

            if (animate)
            {
                var anim = new DoubleAnimation
                {
                    To = targetY,
                    Duration = TimeSpan.FromMilliseconds(250),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                IndicatorTransform.BeginAnimation(TranslateTransform.YProperty, anim);
            }
            else
            {
                IndicatorTransform.Y = targetY;
            }
        }

        private void ShowPage(int index)
        {
            MainContent.Content = index switch
            {
                0 => new GamePage(),
                1 => new DownloadPage(),
                2 => new ModPage(),
                3 => new SettingsPage(),
                _ => null
            };
        }

        // ===== 全局进度条 =====
        public void ShowGlobalProgress(string text)
        {
            GlobalProgressPanel.Visibility = Visibility.Visible;
            GlobalProgressText.Text = text;
        }

        public void UpdateGlobalProgress(double percent, string text)
        {
            GlobalProgressBar.Value = percent;
            if (!string.IsNullOrEmpty(text))
                GlobalProgressText.Text = text;
        }

        public void HideGlobalProgress()
        {
            GlobalProgressPanel.Visibility = Visibility.Collapsed;
            GlobalProgressBar.Value = 0;
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
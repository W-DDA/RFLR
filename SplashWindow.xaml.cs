using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MyLauncher
{
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();
            Loaded += SplashWindow_Loaded;
        }

        private void SplashWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // ===== 1. Logo 淡入 + 缩放 =====
            var logoFade = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            LogoImage.BeginAnimation(OpacityProperty, logoFade);

            var logoScale = new DoubleAnimation
            {
                From = 0.5,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 }
            };
            LogoScale.BeginAnimation(ScaleTransform.ScaleXProperty, logoScale);
            LogoScale.BeginAnimation(ScaleTransform.ScaleYProperty, logoScale);

            // ===== 2. 文字依次淡入 =====
            FadeIn(TitleText, 400, 300);
            FadeIn(AbbrText, 300, 450);
            FadeIn(StatusText, 300, 600);

            // ===== 3. 进度条淡入 + 走完 =====
            FadeIn(LoadProgress, 300, 700);

            var progressAnim = new DoubleAnimation
            {
                From = 0,
                To = 100,
                Duration = TimeSpan.FromSeconds(1.6),
                BeginTime = TimeSpan.FromMilliseconds(800),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            LoadProgress.BeginAnimation(RangeBase.ValueProperty, progressAnim);

            // ===== 4. 窗口整体淡入 =====
            var windowFade = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(350)
            };
            BeginAnimation(OpacityProperty, windowFade);
        }

        private void FadeIn(UIElement element, int durationMs, int beginMs)
        {
            var anim = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                BeginTime = TimeSpan.FromMilliseconds(beginMs),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            element.BeginAnimation(OpacityProperty, anim);
        }
    }
}
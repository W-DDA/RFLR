using System.Windows;
using System.Windows.Input;

namespace MyLauncher
{
    public partial class CrashReportWindow : Window
    {
        public CrashReportWindow(string summary, string detail)
        {
            InitializeComponent();
            SummaryText.Text = summary;
            DetailBox.Text = detail;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Clipboard.SetText(DetailBox.Text);
            System.Windows.MessageBox.Show("已复制到剪贴板", "提示");
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RoboCopy
{
    /// <summary>
    /// Interaction logic for DebugOut.xaml
    /// </summary>
    public partial class LiveStreamWindow : Window
    {
        public string Output
        {
            get {  return this.Dispatcher.Invoke(() => { return outputBox.Text; }); }
            set { this.Dispatcher.Invoke(() => outputBox.Text = value); }
        }

        public void AppendOutput(string s)
        {
            this.Dispatcher.Invoke(() => {
                outputBox.Text += s;
                outputBox.ScrollToEnd();
            });
        }

        public void UpdateProgressBar(double percentage)
        {
            if (percentage < 0 || percentage > 100) return;
            this.Dispatcher.Invoke(() => {
                fileProgress.Value = percentage;
            });
        }

        public LiveStreamWindow()
        {
            InitializeComponent();
        }

        public LiveStreamWindow(string output) : this()
        {
            this.Visibility = Visibility.Visible;
            this.outputBox.Text = output;
        }

        private void outputBox_saveAs_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog();
            dialog.Filter = "Log files (*.log)|*.log|Text files (*.txt)|*.txt";
            dialog.CheckFileExists = false;
            dialog.CheckPathExists = true;
            dialog.OverwritePrompt = true;

            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(dialog.FileName))
                {
                    file.WriteLine(outputBox.Text);
                    file.Flush();
                }
            }
        }

        private void outputBox_copyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(outputBox.Text);
        }
    }
}

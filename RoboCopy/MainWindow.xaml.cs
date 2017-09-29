using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RoboCopy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CancellationTokenSource wtoken;
        private Task task;


        #region Win32 API Stuff

        // Define the Win32 API methods we are going to use
        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern bool InsertMenu(IntPtr hMenu, Int32 wPosition, Int32 wFlags, Int32 wIDNewItem, string lpNewItem);

        /// Define our Constants we will use
        public const Int32 WM_SYSCOMMAND = 0x112;
        public const Int32 MF_SEPARATOR = 0x800;
        public const Int32 MF_BYPOSITION = 0x400;
        public const Int32 MF_STRING = 0x0;

        #endregion

        // The constants we'll use to identify our custom system menu items
        public const Int32 _AboutSysMenuID = 1000;

        /// <summary>
        /// This is the Win32 Interop Handle for this Window
        /// </summary>
        public IntPtr Handle
        {
            get
            {
                return new WindowInteropHelper(this).Handle;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            /// Get the Handle for the Forms System Menu
            IntPtr systemMenuHandle = GetSystemMenu(this.Handle, false);

            /// Create our new System Menu items just before the Close menu item
            InsertMenu(systemMenuHandle, 5, MF_BYPOSITION | MF_SEPARATOR, 0, string.Empty); // <-- Add a menu seperator
            InsertMenu(systemMenuHandle, 7, MF_BYPOSITION, _AboutSysMenuID, "About...");

            // Attach our WndProc handler to this Window
            HwndSource source = HwndSource.FromHwnd(this.Handle);
            source.AddHook(new HwndSourceHook(WndProc));
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Check if a System Command has been executed
            if (msg == WM_SYSCOMMAND)
            {
                // Execute the appropriate code for the System Menu item that was clicked
                switch (wParam.ToInt32())
                {
                    case _AboutSysMenuID:
                        bool createWindow = true;
                        // closing open log windows 
                        foreach (Window w in System.Windows.Application.Current.Windows)
                        {
                            AboutWindow aw = w as AboutWindow;
                            if (aw == null) continue;
                            aw.Focus();
                            createWindow = false;
                        }

                        if(createWindow)
                        {
                            new AboutWindow();
                        }

                        handled = true;
                        break;
                }
            }

            return IntPtr.Zero;
        }


        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
        }

        private string ChoosePath(string path)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (path != null && path.Trim()!="")
            {
                dialog.SelectedPath =  path;
            }
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                return dialog.SelectedPath;
            }

            return null;
        }

        private void syncButton_MouseEnter(object sender, MouseEventArgs e)
        {
            syncButtonImage.RenderTransform = new RotateTransform(45, 38, 38);
        }

        private void syncButton_MouseLeave(object sender, MouseEventArgs e)
        {
            syncButtonImage.RenderTransform = null;

        }


        private void setAllFormsEnabled(bool state)
        {
            // srcPath, destPath, logFilePath
            srcPath.IsEnabled = state;
            srcChooseButton.IsEnabled = state;
            destPath.IsEnabled = state;
            destChooseButton.IsEnabled = state;
            logFilePath.IsEnabled = state;
            logFileChooseButton.IsEnabled = state;

            // copy options
            copyOptionData.IsEnabled = state;
            copyOptionAttr.IsEnabled = state;
            copyOptionTime.IsEnabled = state;
            copyOptionAcl.IsEnabled = state;
            copyOptionOwner.IsEnabled = state;
            copyOptionAudit.IsEnabled = state;

            // retry options
            retryNumber.IsEnabled = state;
            retryWaitSec.IsEnabled = state;
            retryRestart.IsEnabled = state;

            // output options
            liveStreamData.IsEnabled = state;

            // various options
            mirrorCopy.IsEnabled = state;

            syncButton.IsEnabled = state;
        }


        private void startRunningAnimationAndLockForms()
        {
            setAllFormsEnabled(false);
            syncButtonText.Text = "Running sync";

            wtoken = new CancellationTokenSource();
            task = Task.Run(async () =>
            {
                int i = 0;
                while (true)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        i += 10;
                        if (i > 360) i = 10;
                        syncButtonImage.RenderTransform = null;
                        syncButtonImage.RenderTransform = new RotateTransform(i, 38, 38);
                    });

                    await Task.Delay(100, wtoken.Token);
                }
            }, wtoken.Token);
        }

        private async void stopRunningAnimationAndUnlockForms()
        {
            wtoken.Cancel();

            await Task.Delay(200); // give the animation some time to quit

            wtoken.Dispose();
            wtoken = null;
            task = null;

            syncButtonText.Text = "Start sync";
            setAllFormsEnabled(true);
        }

        private bool validateForms(bool showDialog = true)
        {
            string validationMessages = "";
            if (srcPath.Text == null || srcPath.Text == "" || !System.IO.Directory.Exists(srcPath.Text))
            {
                srcPath.Background = new SolidColorBrush(Colors.LightPink);
                srcPath.ToolTip = "Please enter a valid source directory.";
                validationMessages += srcPath.ToolTip + "\n";
            }
            else
            {
                srcPath.ClearValue(TextBox.BackgroundProperty);
                srcPath.ToolTip = null;
            }
            if (destPath.Text == null || destPath.Text == "" || !(System.IO.Directory.Exists(destPath.Text) || System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(destPath.Text))))
            {
                destPath.Background = new SolidColorBrush(Colors.LightPink);
                destPath.ToolTip = "Please enter a valid destination directory.";
                validationMessages += destPath.ToolTip + "\n";
            }
            else
            {
                destPath.ClearValue(TextBox.BackgroundProperty);
                destPath.ToolTip = null;
            }
            if (validationMessages != "" && showDialog)
            {
                System.Windows.Forms.MessageBox.Show(validationMessages.TrimEnd(), "RoboCopy Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation, System.Windows.Forms.MessageBoxDefaultButton.Button1);
                return false;
            }

            return true;
        }

        private async void syncButton_Click(object sender, RoutedEventArgs e)
        {
            if (wtoken == null)
            {
                if (!validateForms()) return;

                // closing open log windows 
                foreach (Window w in System.Windows.Application.Current.Windows)
                {
                    LiveStreamWindow lsw = w as LiveStreamWindow;
                    if (lsw == null) continue;
                    lsw.Close();
                }

                startRunningAnimationAndLockForms();

                try
                {
                    RoboCopyCommand r = new RoboCopyCommand();
                    r.Source = srcPath.Text;
                    r.Destination = destPath.Text;
                    r.LogFile = logFilePath.Text;
                    // mirror mode
                    r.CopyMirror = mirrorCopy.IsChecked.Value;
                    // copy flags
                    r.CopyFlags = 0;
                    if (copyOptionData.IsChecked.Value) r.CopyFlags = r.CopyFlags | RoboCopyCopyFlags.Data;
                    if (copyOptionAttr.IsChecked.Value) r.CopyFlags = r.CopyFlags | RoboCopyCopyFlags.Attributes;
                    if (copyOptionTime.IsChecked.Value) r.CopyFlags = r.CopyFlags | RoboCopyCopyFlags.Timestamps;
                    if (copyOptionAcl.IsChecked.Value) r.CopyFlags = r.CopyFlags | RoboCopyCopyFlags.Acls;
                    if (copyOptionOwner.IsChecked.Value) r.CopyFlags = r.CopyFlags | RoboCopyCopyFlags.OwnerInfo;
                    if (copyOptionAudit.IsChecked.Value) r.CopyFlags = r.CopyFlags | RoboCopyCopyFlags.AuditingInfo;
                    // retries
                    try { r.CopyRetries = int.Parse(retryNumber.Text); } catch(Exception ex) { throw new Exception("Number of retries is not a valid number:\n" + ex.Message); }
                    try { r.CopyRetrySleepSecs = int.Parse(retryWaitSec.Text); } catch (Exception ex) { throw new Exception("Wait time (in s) is not a valid number:\n" + ex.Message); }
                    // restart mode
                    r.CopyRestartMode = retryRestart.IsChecked.Value;


                    LiveStreamWindow lsw = null;
                    // do we have to open a live stream window?
                    if (liveStreamData.IsChecked.Value)
                    {
                        lsw = new LiveStreamWindow("Running command: " + r.RoboCopyExecPath + " " + r.getRobocopyArguments() + "\n");
                        if (logFilePath.Text == null || logFilePath.Text == "") lsw.AppendOutput("Info: No log file has been specified. It is recommended to use a log file!\n");
                        r.DisableStandardDataResult = true;
                        r.OutputDataReceived += (s) => lsw.AppendOutput(s);
                        r.ErrorDataReceived += (s) => lsw.AppendOutput(s);
                        r.FilePercentageUpdate += (d) => lsw.UpdateProgressBar(d);
                    }
                    else
                    {
                        r.DisableStandardDataResult = true; // uses less memory, since we will not show any debug window
                    }

                    RoboCopyCommandResult t = await r.Run();

                    if (liveStreamData.IsChecked.Value && t.ReturnCode > 0)
                    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        // background task to ensure, that the Error Message gets appended to the end (Without this, this message will appear in the beginning)
                        Task.Run(async () => {
                            await Task.Delay(100);
                            if (t.ReturnCode > 4)
                            {
                                lsw.AppendOutput("\n" + "RoboCopy failed with return code: " + t.ReturnCode + "\n(" + t.ReturnCodeFlags + ")\n");
                            }
                            else
                            {
                                lsw.AppendOutput("\n" + "RoboCopy finished with return code: " + t.ReturnCode + "\n(" + t.ReturnCodeFlags + ")\n");
                            }
                        });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    }

                    if (t.ReturnCode >= 4)
                    {
                        throw new Exception("RoboCopy failed with return code: " + t.ReturnCode + "\n(" + t.ReturnCodeFlags + ")");
                    }

                    if (liveStreamData.IsChecked.Value)
                    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        // background task to ensure, that the ProgressBar gets updated to 100 percent (Without this, the running update from the previous job overrides it)
                        Task.Run(async () => {
                            await Task.Delay(100);
                            lsw.UpdateProgressBar(100);
                        });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message, "RoboCopy Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation, System.Windows.Forms.MessageBoxDefaultButton.Button1);
                }
                finally
                {
                    //await Task.Delay(4000);   // Debug - to see the animation

                    stopRunningAnimationAndUnlockForms();
                }
            }
            /*else
            {
                if (!wtoken.IsCancellationRequested)
                {
                    setAllFormsEnabled(true);

                    wtoken.Cancel();
                    // late disposal
                    Task.Run(() =>
                    {
                        Task.Delay(400).Wait();
                        this.Dispatcher.Invoke(() =>
                        {
                            wtoken.Dispose();
                            wtoken = null;
                            task = null;
                        });
                    });
                }
            }*/
        }

        private void logChooseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog();
            dialog.Filter = "Log files (*.log)|*.log|Text files (*.txt)|*.txt";
            dialog.CheckFileExists = false;
            dialog.CheckPathExists = true;
            dialog.OverwritePrompt = true;

            if (logFilePath.Text!=null && logFilePath.Text.Trim()!="")
            {
                
                dialog.FileName = logFilePath.Text;
            }

            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                logFilePath.Text = dialog.FileName;
            }


        }

        private void destChooseButton_Click(object sender, RoutedEventArgs e)
        {
            string s = ChoosePath(destPath.Text);
            if (s != null) destPath.Text = s;
        }

        private void srcChooseButton_Click(object sender, RoutedEventArgs e)
        {
            string s = ChoosePath(srcPath.Text);
            if (s != null) srcPath.Text = s;
        }

        private void logFilePath_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(logFilePath.Text == null || logFilePath.Text == "")
            {
                liveStreamData.IsChecked = true;
            }
            else
            {
                liveStreamData.IsChecked = false;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            App.Current.Shutdown();
        }
    }
}

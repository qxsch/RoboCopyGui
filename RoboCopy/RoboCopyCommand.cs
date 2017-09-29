using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Globalization;

namespace RoboCopy
{
    public class RoboCopyCommand
    {
        public string RoboCopyExecPath = @"C:\Windows\System32\robocopy.exe";
        public string Source;
        public string Destination;
        public string LogFile;

        public RoboCopyCopyFlags CopyFlags = RoboCopyCopyFlags.Data | RoboCopyCopyFlags.Attributes | RoboCopyCopyFlags.Timestamps;
        public bool CopyRestartMode = true;
        public int CopyRetries = 100;
        public int CopyRetrySleepSecs = 30;
        public bool CopyMirror = true;

        public int ProcessId = 0;

        public bool DisableStandardDataResult = false;
        public event Action<string> OutputDataReceived;
        public event Action<string> ErrorDataReceived;
        public event Action<double> FilePercentageUpdate;
        private DateTime lastFilePercentageUpdate = DateTime.Now.AddHours(-1);
        private double lastFilePercentageValue=0;


        public RoboCopyCommand() { }
        public RoboCopyCommand(string Source, string Destination) : this()
        {
            this.Source = Source;
            this.Destination = Destination;
        }


        public string getRobocopyArguments()
        {
            string args = EscapeArgs.EncodeParameterArgument(Source) + " " + EscapeArgs.EncodeParameterArgument(Destination);

            if (CopyFlags == 0) throw new Exception("Invalid CopyFlags. You have to supply at least one flag.");
            args += " /copy:";
            if (CopyFlags.HasFlag(RoboCopyCopyFlags.Data)) args += "D";
            if (CopyFlags.HasFlag(RoboCopyCopyFlags.Attributes)) args += "A";
            if (CopyFlags.HasFlag(RoboCopyCopyFlags.Timestamps)) args += "T";
            if (CopyFlags.HasFlag(RoboCopyCopyFlags.Acls)) args += "S";
            if (CopyFlags.HasFlag(RoboCopyCopyFlags.OwnerInfo)) args += "O";
            if (CopyFlags.HasFlag(RoboCopyCopyFlags.AuditingInfo)) args += "U";

            // just for windows 10 plus
            if(System.Environment.OSVersion.Version.Major>=10)
            {
                args += " /dcopy:";
                if (CopyFlags.HasFlag(RoboCopyCopyFlags.Data)) args += "D";
                if (CopyFlags.HasFlag(RoboCopyCopyFlags.Attributes)) args += "A";
                if (CopyFlags.HasFlag(RoboCopyCopyFlags.Timestamps)) args += "T";
            }

            if (CopyRestartMode) args += " /z";

            if (CopyRetries > 0) args += " /r:" + CopyRetries;
            if (CopyRetrySleepSecs > 0) args += " /w:" + CopyRetrySleepSecs;

            if (LogFile!=null && LogFile.Trim()!="")
            {
                args += " /log:" + EscapeArgs.EncodeParameterArgument(LogFile);
                args += " /np";
                if (!Directory.Exists(Path.GetDirectoryName(LogFile)))
                {
                    throw new Exception("The logfile's parent directory \"" + Path.GetDirectoryName(LogFile) + "\" does not exist.");
                }
            }

            if(CopyMirror)
            {
                args += " /mir";
            }
            else
            {
                args += " /e";
            }

            if (Source==null) throw new Exception("Please supply a source directory.");
            if (Source.Trim() == "") throw new Exception("Please supply a source directory.");
            if (!Directory.Exists(Source))
            {
                throw new Exception("The source directory \"" + Source + "\" does not exist.");
            }

            if (Destination == null) throw new Exception("Please supply a destination directory.");
            if (Destination.Trim()=="") throw new Exception("Please supply a destination directory.");
            if (Path.GetDirectoryName(Destination)!=null && !Directory.Exists(Path.GetDirectoryName(Destination)))
            {
                throw new Exception("The destination's parent directory \"" + Path.GetDirectoryName(Destination) + "\" does not exist.");
            }

            return args;
        }

        private void lazyUpdateFilePercentage(double percentage)
        {
            try
            {
                // same value? -> no update
                if (percentage == lastFilePercentageValue) return;
                // 0 or 100? -> immediate update
                if(percentage==0 || percentage==100)
                {
                    lastFilePercentageUpdate = DateTime.Now;
                    lastFilePercentageValue = percentage;
                    FilePercentageUpdate.Invoke(percentage);
                }
                // at least 1 percent change? -> no update
                if (Math.Abs(percentage - lastFilePercentageValue) < 1) return;

                // check frequency? -> update if ok
                DateTime now = DateTime.Now;
                if(now.Subtract(lastFilePercentageUpdate).TotalMilliseconds > 500)
                {
                    lastFilePercentageUpdate = DateTime.Now;
                    lastFilePercentageValue = percentage;
                    FilePercentageUpdate.Invoke(percentage);
                }
            }
            catch { }
        }

        public Task<RoboCopyCommandResult> Run(CancellationToken cancellationToken = default(CancellationToken))
        {
            lastFilePercentageUpdate = DateTime.Now.AddHours(-1);
            lastFilePercentageValue = 0;
            RoboCopyCommandResult result = new RoboCopyCommandResult();
            TaskCompletionSource<RoboCopyCommandResult> taskSource = new TaskCompletionSource<RoboCopyCommandResult>();

            try
            {
                Process process = new Process();
                process.StartInfo = new ProcessStartInfo()
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    FileName = this.RoboCopyExecPath,
                    Arguments = getRobocopyArguments()
                };

                process.EnableRaisingEvents = true;
                process.Exited += (sender, args) => {
                    lazyUpdateFilePercentage(100);
                    result.ReturnCode = process.ExitCode;
                    process.Dispose();
                    taskSource.TrySetResult(result);
                };

                if (cancellationToken != default(CancellationToken))
                {
                    cancellationToken.Register(() => {
                        lazyUpdateFilePercentage(0);
                        try
                        {
                            process.Kill();
                        }
                        catch { }
                        process.Dispose();
                        taskSource.SetCanceled();
                    });
                }

                Regex perecentageRegex = new Regex(@"^\s*(?<percent>\d{1,3}(.\d+)?)%\s*$");

                if (DisableStandardDataResult)
                {
                    result.StandardOutput = null;
                    result.StandardError = null;
                    process.OutputDataReceived += (sender, e) => {
                        if (e.Data == null) return;
                        Match m = perecentageRegex.Match(e.Data);
                        if (m.Success)
                        {
                            if (FilePercentageUpdate != null) lazyUpdateFilePercentage(Double.Parse(m.Groups["percent"].Value, new CultureInfo("en-US").NumberFormat));
                            return;
                        }
                        else
                        {
                            lazyUpdateFilePercentage(0);
                        }
                        if (OutputDataReceived == null) return;
                        OutputDataReceived.Invoke(e.Data + "\n");
                    };

                    process.ErrorDataReceived += (sender, e) => {
                        if (e.Data == null) return;
                        if (ErrorDataReceived == null) return;
                        ErrorDataReceived.Invoke(e.Data + "\n");
                    };
                }
                else
                {
                    result.StandardOutput = "";
                    result.StandardError = "";
                    process.OutputDataReceived += (sender, e) => {
                        if (e.Data == null) return;
                        Match m = perecentageRegex.Match(e.Data);
                        if (m.Success)
                        {
                            if (FilePercentageUpdate != null) lazyUpdateFilePercentage(Convert.ToDouble(m.Groups["percent"]));
                            return;
                        }
                        else
                        {
                            lazyUpdateFilePercentage(0);
                        }
                        result.StandardOutput += e.Data + "\n";
                        if (OutputDataReceived == null) return;
                        OutputDataReceived.Invoke(e.Data + "\n");
                    };

                    process.ErrorDataReceived += (sender, e) => {
                        if (e.Data == null) return;
                        result.StandardError += e.Data + "\n";
                        if (ErrorDataReceived == null) return;
                        ErrorDataReceived.Invoke(e.Data + "\n");
                    };
                }

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                ProcessId = process.Id;
            }
            catch(Exception e)
            {
                taskSource.TrySetException(e);
            }




            return taskSource.Task;
        }
    }   
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using log4net;

namespace AnyExeService
{
    public class ProcessRunner : IDisposable
    {
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Process process = new Process();

        public string Executable { get; set; }
        public string Argument { get; set; }
        public string WorkingDirectory { get; set; }
        public bool NoWindow { get; set; }

        public event EventHandler Exited {
            add
            {
                this.process.Exited += value;
            }
            remove 
            {
                this.process.Exited -= value;
            }
        }

        public int ExitCode
        {
            get
            {
                return this.process.ExitCode;
            }
        }

        public int ProcessId
        {
            get
            {
                return this.process.Id;
            }
        }

        public void Run()
        {
            logger.InfoFormat("Exe={0}, Argument={1}, WorkingDir={2}", this.Executable, this.Argument, this.WorkingDirectory);

            var startInfo = new ProcessStartInfo();
            startInfo.FileName = this.Executable;
            startInfo.Arguments = this.Argument;
            startInfo.CreateNoWindow = this.NoWindow;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = false;
            startInfo.RedirectStandardError = false;
            startInfo.RedirectStandardInput = false;
            if (this.WorkingDirectory != null && this.WorkingDirectory != "")
            {
                startInfo.WorkingDirectory = this.WorkingDirectory;
            }

            this.process.EnableRaisingEvents = true;
            this.process.StartInfo = startInfo;
            this.process.Start();
        }

        public void Stop()
        {
            if (!this.process.HasExited)
            {
                logger.InfoFormat("Kill PID={0}, Exe={1}", this.ProcessId, this.Executable);
                this.process.Kill();
            }
        }

        public void WaitForExit()
        {
            if (!this.process.HasExited)
            {
                this.process.WaitForExit();
            }
        }

        public void Dispose()
        {
            this.Stop();
            this.process.Dispose();
        }
    }
}

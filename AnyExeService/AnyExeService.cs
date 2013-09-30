using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Reflection;
using System.Configuration;

using log4net;

namespace AnyExeService
{
    /// <summary>
    /// 任意のexeをサービスとして実行する
    /// </summary>
    public partial class AnyExeService : ServiceBase
    {
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private ProcessRunner runner = null;
        private SERVICE_STATUS state = new SERVICE_STATUS();

        public AnyExeService()
        {
            InitializeComponent();

            if (this.DesignMode)
            {
                return;
            }

            this.CanPauseAndContinue = false;
            this.CanShutdown = true;
            this.CanStop = true;
            this.AutoLog = true;

            var setting = this.GetSettings();
            this.ServiceName = setting["ServiceName"].Value;

            this.state.dwWin32ExitCode = 0;
            this.state.dwServiceSpecificExitCode = 0;
            this.state.dwWaitHint = 0;
            this.state.dwCheckPoint = 0;
            // STOP+SHUTDOWN
            this.state.dwControlsAccepted = (int)(ServiceControlesAccepted.SERVICE_ACCEPT_STOP | ServiceControlesAccepted.SERVICE_ACCEPT_SHUTDOWN);
            // SERVICE_WIN32_OWN_PROCESS
            this.state.dwServiceType = 16;

            this.EventLog.Source = this.ServiceName;
        }


        protected override void OnStart(string[] args)
        {
            this.OnStop();

            var setting = this.GetSettings();

            var executable = setting["Executable"].Value;
            var runner = new ProcessRunner()
            {
                Executable = executable,
                Argument = setting["Argument"].Value,
                WorkingDirectory = setting["WorkingDirectory"].Value,
                NoWindow = false,
            };

            runner.Exited += (sender, ev) =>
            {
                var ec = runner.ExitCode;
                this.SetServiceStateWithExitCode(ServiceState.SERVICE_STOPPED, ec);
                var mes = String.Format("PID={0}: {1} exit={2}", this.runner.ProcessId, executable, ec);
                this.EventLog.WriteEntry(mes, EventLogEntryType.Information, 3);
                logger.Info(mes);
            };

            this.runner = runner;

            try
            {
                this.runner.Run();

                var mes = String.Format("PID={0}: {1}", this.runner.ProcessId, executable);
                this.EventLog.WriteEntry(mes, EventLogEntryType.Information, 2);
                logger.Info(mes);
            }
            catch (Exception e)
            {
                var mes = String.Format("*** Failed to execute {0}", executable);
                this.EventLog.WriteEntry(mes, EventLogEntryType.Information, 4);
                logger.Info(mes, e);

                throw;
            }
        }

        protected override void OnStop()
        {
            if (this.runner == null)
            {
                return;
            }

            try
            {
                this.runner.Dispose();
            }
            catch (Exception ex)
            {
                // SCM上停止はできるようにしておきたい。例外無視
                this.EventLog.WriteEntry(ex.Message, EventLogEntryType.Warning, 1);
                logger.Info("*** Exception was raised at Stop()", ex);
            }
            this.runner = null;
        }

        protected override void OnShutdown()
        {
            this.OnStop();
        }

        /// <summary>
        /// サービスの状態を、サービス固有のコード付きで更新する（SCMに通知する）
        /// </summary>
        /// <param name="state"></param>
        /// <param name="code"></param>
        private void SetServiceStateWithExitCode(ServiceState state, int code)
        {
            // ERROR_SERVICE_SPECIFIC_ERROR
            this.state.dwWin32ExitCode = 1066;
            this.state.dwServiceSpecificExitCode = code;
            this.state.dwCurrentState = (int)state;

            ServiceUtil.SetServiceStatus(this.ServiceHandle, ref this.state);
        }

        /// <summary>
        /// サービスの状態を更新する（SCMに通知する）
        /// </summary>
        /// <param name="state"></param>
        private void SetServiceState(ServiceState state)
        {
            this.state.dwCurrentState = (int)state;
            ServiceUtil.SetServiceStatus(this.ServiceHandle, ref this.state);
        }


        /// <summary>
        /// appc.configのappSettingsセクションを返す。
        /// なぜ、このセクションを使っているかというと、
        /// このアセンブリがInstallUtil.exeからロードされたときにProperties.Settingsとして読み込ませられなかったから。
        /// </summary>
        /// <returns></returns>
        private KeyValueConfigurationCollection GetSettings()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var conf = ConfigurationManager.OpenExeConfiguration(assemblyLocation);
            var setting = conf.AppSettings.Settings;

            return setting;
        }
    }
}

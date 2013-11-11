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
using System.Timers;

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

        /// <summary>
        /// プロセスがExitしたときに再起動してもよい状態かどうか
        /// サービスの停止イベントで、プロセスを自分でterminateしたときに、さらに再起動させないため
        /// </summary>
        private bool allowRestart = true;

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

        /// <summary>
        /// サービス開始イベント
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            logger.Info("OnStart()");
            this.allowRestart = true;

            this.OnStopProc();

            var setting = this.GetSettings();

            var restartEternal = false;
            bool.TryParse(this.GetSettingValue(setting, "Restart", null), out restartEternal);
            var executable = setting["Executable"].Value;
            var argument = setting["Argument"].Value;
            var workingDirectory = setting["WorkingDirectory"].Value;

            Func<ProcessRunner> newRunnerFunc = () =>
            {
                var r = new ProcessRunner()
                {
                    Executable = executable,
                    Argument = argument,
                    WorkingDirectory = workingDirectory,
                    NoWindow = true,
                };
                return r;
            };

            this.StartRunner(CreateRunner(newRunnerFunc, restartEternal));
        }


        /// <summary>
        /// プロセス開始
        /// </summary>
        /// <param name="r"></param>
        private void StartRunner(ProcessRunner r)
        {
            this.runner = r;

            try
            {
                this.runner.Run();

                var mes = string.Format("PID={0}: {1}", this.runner.ProcessId, r.Executable);
                this.EventLog.WriteEntry(mes, EventLogEntryType.Information, 2);
                logger.Info(mes);
            }
            catch (Exception e)
            {
                var mes = string.Format("*** Failed to execute {0}", r.Executable);
                this.EventLog.WriteEntry(mes, EventLogEntryType.Information, 4);
                logger.Info(mes, e);

                throw;
            }
        }

        /// <summary>
        /// ExitイベントをattachしたProcessRunnerを作成して返す
        /// </summary>
        /// <param name="newRunnerFunc">ProcessRunnerをインスタンス化して返す関数</param>
        /// <param name="restartEternal">Exit後再起動するかどうか</param>
        /// <returns></returns>
        private ProcessRunner CreateRunner(Func<ProcessRunner> newRunnerFunc, bool restartEternal)
        {
            var newRunner = newRunnerFunc();

            newRunner.Exited += (sender, ev) =>
            {
                var ec = newRunner.ExitCode;
                var mes = string.Format("PID={0}: {1} exit={2}", this.runner.ProcessId, newRunner.Executable, ec);
                this.EventLog.WriteEntry(mes, EventLogEntryType.Information, 3);
                logger.Info(mes);

                this.runner = null;

                if (restartEternal && this.allowRestart)
                {
                    var timer = new Timer()
                    {
                        AutoReset = false,
                        Enabled = true,
                        Interval = 10 * 1000,
                    };

                    timer.Elapsed += (timeSender, timeEv) =>
                    {
                        if (!this.allowRestart || this.runner != null)
                        {
                            return;
                        }

                        // プロセス再起動
                        var nextRunner = this.CreateRunner(newRunnerFunc, restartEternal);
                        this.StartRunner(nextRunner);
                        timer.Dispose();
                    };

                }
                else
                {
                    this.SetServiceStateWithExitCode(ServiceState.SERVICE_STOPPED, ec);
                }
            };

            return newRunner;
        }

        /// <summary>
        /// サービス停止イベント
        /// </summary>
        protected override void OnStop()
        {
            logger.Info("OnStop()");
            this.allowRestart = false;
            this.OnStopProc();
        }

        /// <summary>
        /// OnStop時にやること。
        /// 実行中のプロセスを強制終了
        /// </summary>
        private void OnStopProc()
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
            logger.Info("OnShutdown()");
            this.OnStopProc();
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


        /// <summary>
        /// 設定値コレクションから値を返す。
        /// キーが存在しない場合に、指定したデフォルト値を返す
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private string GetSettingValue(KeyValueConfigurationCollection collection, string key, string defaultValue)
        {
            var v = collection[key];
            return v == null ? defaultValue : v.Value;
        }
    }
}

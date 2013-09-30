using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.IO;

using log4net;


namespace AnyExeService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public ProjectInstaller()
        {
            InitializeComponent();

            if (this.DesignMode)
            {
                return;
            }


            try
            {
                /*
                 * なぜ、このappSettingsを使っているかというと、
                 * このアセンブリがInstallUtil.exeからロードされたときにProperties.Settingsとして読み込ませられなかったから。
                 */
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                var conf = ConfigurationManager.OpenExeConfiguration(assemblyLocation);
                var setting = conf.AppSettings.Settings;

                this.serviceInstaller.ServiceName = setting["ServiceName"].Value;
                this.serviceInstaller.DisplayName = setting["DisplayName"].Value;
                this.serviceInstaller.Description = setting["Description"].Value;
                this.serviceInstaller.StartType = ServiceUtil.GetServiceStartMode(setting["StartMode"].Value);

                this.serviceProcessInstaller.Account = ServiceUtil.GetServiceAccount(setting["ServiceAccount"].Value);
                if (this.serviceProcessInstaller.Account == System.ServiceProcess.ServiceAccount.User)
                {
                    var userName = setting["UserName"].Value;
                    var password = setting["Password"].Value;
                    if (!string.IsNullOrEmpty(userName))
                    {
                        this.serviceProcessInstaller.Username = userName;
                        this.serviceProcessInstaller.Password = password;
                    }
                }

            }
            catch (Exception e)
            {
                logger.Error("*** Some error occurred", e);
                throw;
            }

            this.BeforeInstall += (o, ev) =>
            {
                // サービス名はフレームワークの方で必須チェックしてくれる

                if (string.IsNullOrWhiteSpace(this.serviceInstaller.DisplayName))
                {
                    throw new ApplicationException("*** DisplayName must be specified.");
                }
            };
        }

    }
}

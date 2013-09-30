using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace AnyExeService
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        static void Main()
        {
            var servicesToRun = new ServiceBase[] 
			{ 
				new AnyExeService() 
			};
            ServiceBase.Run(servicesToRun);
        }
    }
}

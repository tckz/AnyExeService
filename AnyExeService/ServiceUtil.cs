using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceProcess;
using System.Runtime.InteropServices;

namespace AnyExeService
{
    /// <summary>
    /// SERVICE_STATUS構造体
    /// windows.h
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SERVICE_STATUS
    {
        public int dwServiceType;
        public int dwCurrentState;
        public int dwControlsAccepted;
        public int dwWin32ExitCode;
        public int dwServiceSpecificExitCode;
        public int dwCheckPoint;
        public int dwWaitHint;
    }

    /// <summary>
    /// SERVICE_STATUS構造体のdwControlsAccepted
    /// </summary>
    [Flags]
    public enum ServiceControlesAccepted : int
    {
        SERVICE_ACCEPT_STOP = 0x00000001,
        SERVICE_ACCEPT_PAUSE_CONTINUE = 0x00000002,
        SERVICE_ACCEPT_SHUTDOWN = 0x00000004,
        SERVICE_ACCEPT_PARAMCHANGE = 0x00000008,
        SERVICE_ACCEPT_NETBINDCHANGE = 0x00000010,
        SERVICE_ACCEPT_HARDWAREPROFILECHANGE = 0x00000020,
        SERVICE_ACCEPT_POWEREVENT = 0x00000040,
        SERVICE_ACCEPT_SESSIONCHANGE = 0x00000080,
    }


    /// <summary>
    /// サービス状態
    /// windows.h
    /// </summary>
    public enum ServiceState : int
    {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007,
    }

    public static class ServiceUtil
    {
        [DllImport("ADVAPI32.DLL", EntryPoint = "SetServiceStatus", SetLastError = true)]
        public static extern bool SetServiceStatus(IntPtr hServiceStatus, ref SERVICE_STATUS lpServiceStatus);

        /// <summary>
        /// enumで選択可能な値を文字列表現で返す
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static string MakeShouldBe(Type e)
        {
            return string.Join("|", Enum.GetNames(e));
        }

        /// <summary>
        /// ServiceAccount下の定義（LocalService、Userなど）の文字列表現からenum値を得る
        /// 該当するenum値がなければ例外
        /// 大文字小文字無視
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static ServiceAccount GetServiceAccount(string s)
        {
            return GetEnumValueFromString<ServiceAccount>(s, true);
        }

        /// <summary>
        /// ServiceStartMode下の定義（Automatic、Manualなど）の文字列表現からenum値を得る
        /// 該当するenum値がなければ例外
        /// 大文字小文字無視
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static ServiceStartMode GetServiceStartMode(string s)
        {
            return GetEnumValueFromString<ServiceStartMode>(s, true);
        }

        /// <summary>
        /// あるenum定義の文字列表現からenum値を得る
        /// 該当するenum値がなければ例外
        /// </summary>
        /// <typeparam name="TEnum">enum型</typeparam>
        /// <param name="s"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        private static TEnum GetEnumValueFromString<TEnum>(string s, bool ignoreCase) where TEnum : struct
        {
            TEnum ret;
            if (!Enum.TryParse<TEnum>(s, ignoreCase, out ret))
            {
                var t = typeof(TEnum);
                throw new ApplicationException(
                    string.Format(
                        "Unknown {0}: {1}, should be {2}",
                        t.Name,
                        s,
                        MakeShouldBe(t)
                    )
                );
            }

            return ret;

        }

    }
}

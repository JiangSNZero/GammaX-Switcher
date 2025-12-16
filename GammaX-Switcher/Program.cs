using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace GammaX_Switcher
{
    internal static class Program
    {
        // 1. 定义唯一互斥锁（替换为你自己的GUID，避免冲突）
        private static readonly string MutexName = "Global\\GammaX_8169B203-FC51-4796-8EFC-E1085BD4B944";
        private static Mutex _appMutex;

        [STAThread]
        static void Main()
        {
            // 2. 单实例校验核心逻辑
            // 创建互斥锁：true=获取初始所有权，isNewInstance=是否为新实例
            _appMutex = new Mutex(true, MutexName, out var isNewInstance);

            if (!isNewInstance)
            {
                // 已有实例运行 → 激活已有窗口并退出当前实例
                ActivateExistingWindow();
                return;
            }

            // 3. 无已有实例 → 执行你原有启动逻辑
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Window());

            // 4. 程序退出时释放互斥锁（防止残留）
            GC.KeepAlive(_appMutex); // 防止GC提前回收互斥锁
            _appMutex.ReleaseMutex();
            _appMutex.Dispose();
        }

        // 3. 激活已有实例的窗口（兼容正常窗口/托盘隐藏窗口）
        private static void ActivateExistingWindow()
        {
            Process currentProcess = Process.GetCurrentProcess();
            // 遍历同名进程（排除当前进程）
            foreach (Process process in Process.GetProcessesByName(currentProcess.ProcessName))
            {
                if (process.Id != currentProcess.Id)
                {
                    IntPtr mainWindowHandle = process.MainWindowHandle;
                    if (mainWindowHandle != IntPtr.Zero)
                    {
                        // 情况1：主窗口可见 → 还原并置顶
                        ShowWindowAsync(mainWindowHandle, SwRestore);
                        SetForegroundWindow(mainWindowHandle);
                    }
                    else
                    {
                        // 情况2：主窗口隐藏（托盘运行）→ 枚举所有窗口找到目标进程
                        EnumWindows((hwnd, lParam) =>
                        {
                            GetWindowThreadProcessId(hwnd, out var processId);
                            if (processId == process.Id)
                            {
                                ShowWindowAsync(hwnd, SwRestore);
                                SetForegroundWindow(hwnd);
                                return false; // 找到后停止枚举
                            }
                            return true;
                        }, IntPtr.Zero);
                    }
                    break;
                }
            }
        }

        // 枚举窗口的委托
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        // 窗口还原常量（最小化时还原）
        private const int SwRestore = 9;
    }
}
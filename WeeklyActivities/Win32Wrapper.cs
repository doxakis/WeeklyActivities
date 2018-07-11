using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WeeklyActivities
{
    public class Win32Wrapper
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        public static string GetCaptionOfActiveWindow()
        {
            string strTitle = string.Empty;
            IntPtr handle = GetForegroundWindow();
            // Obtain the length of the text
            int intLength = GetWindowTextLength(handle) + 1;
            StringBuilder stringBuilder = new StringBuilder(intLength);
            if (GetWindowText(handle, stringBuilder, intLength) > 0)
            {
                strTitle = stringBuilder.ToString();
            }
            return strTitle;
        }

        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        private static LASTINPUTINFO lastInPutNfo;

        static Win32Wrapper()
        {
            lastInPutNfo = new LASTINPUTINFO();
            lastInPutNfo.cbSize = (uint)Marshal.SizeOf(lastInPutNfo);
        }

        [DllImport("User32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        public static uint GetLastInputTime()
        {
            if (!GetLastInputInfo(ref lastInPutNfo))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            return lastInPutNfo.dwTime;
        }

        public static string GetActiveProcess()
        {
            string app = "";
            var foregroundProcess = Process.GetProcessById(GetWindowProcessId(GetForegroundWindow()));
            if (foregroundProcess.ProcessName == "ApplicationFrameHost")
            {
                foregroundProcess = GetRealProcess(foregroundProcess);
            }
            if (foregroundProcess != null)
            {
                app = foregroundProcess.ProcessName;
            }
            return app;
        }

        static Process _realProcess;

        public static Process GetRealProcess(Process foregroundProcess)
        {
            EnumChildWindows(foregroundProcess.MainWindowHandle, ChildWindowCallback, IntPtr.Zero);
            return _realProcess;
        }

        private static bool ChildWindowCallback(IntPtr hwnd, IntPtr lparam)
        {
            var process = Process.GetProcessById(GetWindowProcessId(hwnd));
            if (process.ProcessName != "ApplicationFrameHost")
            {
                _realProcess = process;
            }
            return true;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        public delegate bool WindowEnumProc(IntPtr hwnd, IntPtr lparam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr hwnd, WindowEnumProc callback, IntPtr lParam);

        public static int GetWindowProcessId(IntPtr hwnd)
        {
            int pid;
            GetWindowThreadProcessId(hwnd, out pid);
            return pid;
        }
    }
}

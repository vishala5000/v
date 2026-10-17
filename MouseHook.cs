using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace UniversalRecorder
{
    public class MouseHook
    {
        private const int WH_MOUSE_LL = 14, WM_LBUTTONDOWN = 0x0201;
        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string lpModuleName);
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        [StructLayout(LayoutKind.Sequential)] private struct POINT { public int x, y; }
        [StructLayout(LayoutKind.Sequential)] private struct MSLLHOOKSTRUCT { public POINT pt; public uint mouseData, flags, time; public IntPtr dwExtraInfo; }
        private IntPtr hookId = IntPtr.Zero; private LowLevelMouseProc proc;
        public event Action<int, int> LeftButtonDown;
        public MouseHook() { proc = HookCallback; }
        public void Install() { using (ProcessModule m = Process.GetCurrentProcess().MainModule) hookId = SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(m.ModuleName), 0); }
        public void Uninstall() { UnhookWindowsHookEx(hookId); }
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam.ToInt32() == WM_LBUTTONDOWN)
            {
                MSLLHOOKSTRUCT hook = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                LeftButtonDown?.Invoke(hook.pt.x, hook.pt.y);
            }
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }
    }
}
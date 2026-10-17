using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace UniversalRecorder
{
    public class MouseHook
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_MOUSEMOVE = 0x0200;
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        
        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT { public POINT pt; public uint mouseData; public uint flags; public uint time; public IntPtr dwExtraInfo; }
        
        private IntPtr hookId = IntPtr.Zero;
        private LowLevelMouseProc proc;
        
        public event Action<int, int> LeftButtonDown;
        public event Action<int, int> LeftButtonUp;
        public event Action<int, int> MouseMove;
        
        public MouseHook()
        {
            proc = HookCallback;
        }
        
        public void Install()
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                hookId = SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }
        
        public void Uninstall()
        {
            UnhookWindowsHookEx(hookId);
        }
        
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                int x = hookStruct.pt.x;
                int y = hookStruct.pt.y;
                
                int msg = wParam.ToInt32();
                if (msg == WM_LBUTTONDOWN) LeftButtonDown?.Invoke(x, y);
                else if (msg == WM_LBUTTONUP) LeftButtonUp?.Invoke(x, y);
                else if (msg == WM_MOUSEMOVE) MouseMove?.Invoke(x, y);
            }
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }
    }
}
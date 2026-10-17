using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace UniversalRecorder
{
    public class WindowTracker
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }
        
        private DispatcherTimer timer;
        public IntPtr ActiveWindow { get; private set; }
        
        public void Start()
        {
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            timer.Tick += Timer_Tick;
            timer.Start();
        }
        
        public void Stop()
        {
            timer?.Stop();
        }
        
        private void Timer_Tick(object sender, EventArgs e)
        {
            IntPtr hWnd = GetForegroundWindow();
            if (hWnd != ActiveWindow)
            {
                ActiveWindow = hWnd;
            }
        }
    }
}
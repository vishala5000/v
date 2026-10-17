using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;
namespace UniversalRecorder
{
    public class WindowTracker
    {
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        private DispatcherTimer timer;
        public IntPtr ActiveWindow { get; private set; }
        public void Start() { timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) }; timer.Tick += (s, e) => ActiveWindow = GetForegroundWindow(); timer.Start(); }
        public void Stop() { timer?.Stop(); }
    }
}
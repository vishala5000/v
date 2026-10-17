using System;
using System.Media;
using System.Threading;

namespace UniversalRecorder
{
    public static class AudioPlayer
    {
        private static Thread soundThread;
        
        public static void Initialize()
        {
            // Nothing to initialize for system beep
        }
        
        public static void PlayClick()
        {
            soundThread = new Thread(() =>
            {
                try
                {
                    SystemSounds.Beep.Play();
                }
                catch { }
            });
            soundThread.IsBackground = true;
            soundThread.Start();
        }
    }
}
using System.Media;
using System.Threading;
namespace UniversalRecorder
{
    public static class AudioPlayer
    {
        public static void Initialize() { }
        public static void PlayClick()
        {
            Thread t = new Thread(() => { try { SystemSounds.Beep.Play(); } catch { } });
            t.IsBackground = true; t.Start();
        }
    }
}
using System;
using System.Diagnostics;
using System.IO;
namespace UniversalRecorder
{
    public class Recorder
    {
        private Process ffmpegProcess;
        public string OutputPath { get; private set; }
        public Recorder(string outputPath, bool autoZoom) { OutputPath = outputPath; }
        public void Start()
        {
            string ffmpegPath = FindFFmpeg();
            if (string.IsNullOrEmpty(ffmpegPath)) throw new Exception("FFmpeg not found in app folder.");
            ffmpegProcess = new Process { StartInfo = new ProcessStartInfo { FileName = ffmpegPath, Arguments = $"-f gdigrab -framerate 30 -i desktop -c:v libx264 -preset ultrafast -crf 23 -pix_fmt yuv420p -movflags +faststart -y \"{OutputPath}\"", UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true } };
            ffmpegProcess.Start();
        }
        public void Stop()
        {
            if (ffmpegProcess != null && !ffmpegProcess.HasExited)
            {
                try { ffmpegProcess.StandardInput.WriteLine("q"); } catch { }
                if (!ffmpegProcess.WaitForExit(5000)) ffmpegProcess.Kill();
            }
        }
        private string FindFFmpeg()
        {
            string local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
            if (File.Exists(local)) return local;
            foreach (string path in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
                if (File.Exists(Path.Combine(path, "ffmpeg.exe"))) return Path.Combine(path, "ffmpeg.exe");
            return "";
        }
    }
}
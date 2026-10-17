using System;
using System.Diagnostics;
using System.IO;

namespace UniversalRecorder
{
    public class Recorder
    {
        private Process ffmpegProcess;
        public string OutputPath { get; private set; }
        private bool autoZoom;
        
        public Recorder(string outputPath, bool autoZoom)
        {
            OutputPath = outputPath;
            this.autoZoom = autoZoom;
        }
        
        public void Start()
        {
            string ffmpegPath = FindFFmpeg();
            if (string.IsNullOrEmpty(ffmpegPath))
            {
                throw new Exception("FFmpeg not found. Please ensure ffmpeg.exe is in the same folder as this application.");
            }
            
            string args = $"-f gdigrab -framerate 30 -i desktop " +
                         $"-c:v libx264 -preset ultrafast -crf 23 " +
                         $"-pix_fmt yuv420p -movflags +faststart " +
                         $"-y \"{OutputPath}\"";
            
            ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true
                }
            };
            
            ffmpegProcess.Start();
        }
        
        public void Stop()
        {
            if (ffmpegProcess != null && !ffmpegProcess.HasExited)
            {
                try
                {
                    ffmpegProcess.StandardInput.WriteLine("q");
                }
                catch { }
                
                if (!ffmpegProcess.WaitForExit(5000))
                {
                    ffmpegProcess.Kill();
                }
            }
        }
        
        private string FindFFmpeg()
        {
            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
            if (File.Exists(localPath)) return localPath;
            
            string pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (pathEnv != null)
            {
                foreach (string path in pathEnv.Split(Path.PathSeparator))
                {
                    string fullPath = Path.Combine(path, "ffmpeg.exe");
                    if (File.Exists(fullPath)) return fullPath;
                }
            }
            
            return "";
        }
    }
}
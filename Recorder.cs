using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;

namespace UniversalRecorder
{
    public class Recorder
    {
        private Process ffmpegProcess;
        private Thread captureThread;
        private bool isRecording = false;
        private WindowTracker windowTracker;
        public string OutputPath { get; private set; }
        private bool autoZoom;
        private Rectangle currentZoomRect;

        public Recorder(string outputPath, bool autoZoom, WindowTracker tracker)
        {
            OutputPath = outputPath;
            this.autoZoom = autoZoom;
            this.windowTracker = tracker;
        }

        public void Start()
        {
            string ffmpegPath = FindFFmpeg();
            if (string.IsNullOrEmpty(ffmpegPath)) throw new Exception("FFmpeg not found in app folder.");

            // Pipe raw BGR24 video at 1280x720, 30 FPS to FFmpeg
            string args = $"-y -f rawvideo -vcodec rawvideo -s 1280x720 -r 30 -pix_fmt bgr24 -i - " +
                          $"-c:v libx264 -preset ultrafast -crf 23 -pix_fmt yuv420p -movflags +faststart \"{OutputPath}\"";

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
            isRecording = true;
            captureThread = new Thread(CaptureLoop) { IsBackground = true };
            captureThread.Start();
        }

        private void CaptureLoop()
        {
            int width = 1280, height = 720;
            int stride = width * 3;
            byte[] buffer = new byte[stride * height];
            Rectangle screenRect = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            currentZoomRect = screenRect;

            while (isRecording)
            {
                Rectangle targetRect = screenRect;
                if (autoZoom && windowTracker != null && windowTracker.ActiveWindow != IntPtr.Zero)
                {
                    var bounds = windowTracker.ActiveWindowBounds;
                    // Add 20% padding around the active window
                    int padW = Math.Max(50, bounds.Width / 5);
                    int padH = Math.Max(50, bounds.Height / 5);
                    targetRect = new Rectangle(
                        Math.Max(0, bounds.X - padW),
                        Math.Max(0, bounds.Y - padH),
                        Math.Min(screenRect.Width - (bounds.X - padW), bounds.Width + padW * 2),
                        Math.Min(screenRect.Height - (bounds.Y - padH), bounds.Height + padH * 2)
                    );
                }

                // Smooth zoom interpolation (lerp)
                currentZoomRect.X += (targetRect.X - currentZoomRect.X) / 8;
                currentZoomRect.Y += (targetRect.Y - currentZoomRect.Y) / 8;
                currentZoomRect.Width += (targetRect.Width - currentZoomRect.Width) / 8;
                currentZoomRect.Height += (targetRect.Height - currentZoomRect.Height) / 8;

                // Capture and scale
                using (Bitmap frame = new Bitmap(width, height, PixelFormat.Format24bppRgb))
                using (Graphics g = Graphics.FromImage(frame))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.CopyFromScreen(currentZoomRect.X, currentZoomRect.Y, 0, 0, currentZoomRect.Size);
                    g.DrawImage(frame, new Rectangle(0, 0, width, height), new Rectangle(0, 0, currentZoomRect.Width, currentZoomRect.Height), GraphicsUnit.Pixel);

                    // Convert to byte array (BGR24)
                    BitmapData bmpData = frame.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                    System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, buffer, 0, buffer.Length);
                    frame.UnlockBits(bmpData);
                }

                // Write to FFmpeg
                try { ffmpegProcess.StandardInput.BaseStream.Write(buffer, 0, buffer.Length); }
                catch { break; }

                Thread.Sleep(33); // ~30 FPS
            }
        }

        public void Stop()
        {
            isRecording = false;
            if (captureThread != null) captureThread.Join(1000);
            
            if (ffmpegProcess != null && !ffmpegProcess.HasExited)
            {
                // Closing StandardInput sends EOF to FFmpeg, guaranteeing a perfectly saved MP4!
                ffmpegProcess.StandardInput.Close();
                ffmpegProcess.WaitForExit(3000);
                if (!ffmpegProcess.HasExited) ffmpegProcess.Kill();
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
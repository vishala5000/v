using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;

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
        private RectangleF currentZoomRectF;
        private string logFile;

        public Recorder(string outputPath, bool autoZoom, WindowTracker tracker)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            OutputPath = Path.Combine(desktopPath, $"recording_{timestamp}.mp4");
            
            this.autoZoom = autoZoom;
            this.windowTracker = tracker;
            logFile = Path.Combine(desktopPath, $"recorder_log_{timestamp}.txt");
            Log($"Output path: {OutputPath}");
        }

        private void Log(string message)
        {
            try { File.AppendAllText(logFile, $"[{DateTime.Now:HH:mm:ss}] {message}\n"); } catch { }
        }

        public void Start()
        {
            string ffmpegPath = FindFFmpeg();
            if (string.IsNullOrEmpty(ffmpegPath))
            {
                Log("ERROR: FFmpeg not found!");
                throw new Exception("FFmpeg not found in app folder.");
            }
            
            Log($"FFmpeg found at: {ffmpegPath}");

            // STRICT 20 FPS input to guarantee real-time playback speed
            string args = $"-y -f rawvideo -vcodec rawvideo -framerate 20 -s 1280x720 -pix_fmt bgr24 -i - " +
                          $"-c:v libx264 -preset ultrafast -crf 23 -pix_fmt yuv420p -movflags +faststart \"{OutputPath}\"";
            
            Log($"FFmpeg args: {args}");

            ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                },
                EnableRaisingEvents = true
            };
            
            ffmpegProcess.ErrorDataReceived += (s, e) => { if (e.Data != null) Log($"FFmpeg: {e.Data}"); };
            
            ffmpegProcess.Start();
            ffmpegProcess.BeginErrorReadLine();
            
            Log("FFmpeg process started");
            
            isRecording = true;
            captureThread = new Thread(CaptureLoop) { IsBackground = true };
            captureThread.Start();
            
            Log("Capture thread started");
        }

        private void CaptureLoop()
        {
            int width = 1280, height = 720;
            int bufferSize = width * height * 3;
            byte[] buffer = new byte[bufferSize];
            Rectangle screenRect = Screen.PrimaryScreen.Bounds;
            
            currentZoomRectF = new RectangleF(screenRect.X, screenRect.Y, screenRect.Width, screenRect.Height);
            int frameCount = 0;
            
            // Stopwatch for strict framerate enforcement
            Stopwatch sw = new Stopwatch();
            long targetFrameTimeMs = 50; // 1000ms / 20 FPS = 50ms per frame

            Log($"Screen size: {screenRect.Width}x{screenRect.Height}");

            while (isRecording)
            {
                sw.Restart();
                try
                {
                    RectangleF targetRectF;
                    if (autoZoom && windowTracker != null && windowTracker.ActiveWindow != IntPtr.Zero)
                    {
                        var bounds = windowTracker.ActiveWindowBounds;
                        
                        // Add 20% padding around the active window
                        float padW = bounds.Width * 0.2f;
                        float padH = bounds.Height * 0.2f;
                        float targetW = bounds.Width + padW * 2;
                        float targetH = bounds.Height + padH * 2;
                        
                        // ENFORCE 16:9 ASPECT RATIO to guarantee ZERO stretching when scaled to 1280x720
                        float targetAspect = 1280f / 720f;
                        float currentAspect = targetW / targetH;
                        
                        if (currentAspect > targetAspect)
                        {
                            targetH = targetW / targetAspect; // Expand height
                        }
                        else
                        {
                            targetW = targetH * targetAspect; // Expand width
                        }
                        
                        // Center the expanded box on the active window
                        float x = bounds.X - (targetW - bounds.Width) / 2f;
                        float y = bounds.Y - (targetH - bounds.Height) / 2f;
                        
                        // Clamp to screen bounds so we don't capture black desktop areas
                        x = Math.Max(0, Math.Min(x, screenRect.Width - targetW));
                        y = Math.Max(0, Math.Min(y, screenRect.Height - targetH));
                        
                        targetRectF = new RectangleF(x, y, targetW, targetH);
                    }
                    else
                    {
                        // Full screen (1366x768 to 1280x720 is a 0.05% difference, visually imperceptible, no black bars)
                        targetRectF = new RectangleF(screenRect.X, screenRect.Y, screenRect.Width, screenRect.Height);
                    }

                    // Smooth lerp (15% per frame)
                    currentZoomRectF.X += (targetRectF.X - currentZoomRectF.X) * 0.15f;
                    currentZoomRectF.Y += (targetRectF.Y - currentZoomRectF.Y) * 0.15f;
                    currentZoomRectF.Width += (targetRectF.Width - currentZoomRectF.Width) * 0.15f;
                    currentZoomRectF.Height += (targetRectF.Height - currentZoomRectF.Height) * 0.15f;

                    Rectangle captureRect = Rectangle.Round(currentZoomRectF);

                    // STEP 1: Capture screen into source bitmap
                    using (Bitmap sourceBitmap = new Bitmap(captureRect.Width, captureRect.Height, PixelFormat.Format24bppRgb))
                    using (Graphics sourceGraphics = Graphics.FromImage(sourceBitmap))
                    {
                        sourceGraphics.CopyFromScreen(captureRect.X, captureRect.Y, 0, 0, captureRect.Size);
                        
                        // STEP 2: Scale source bitmap into destination bitmap (1280x720)
                        using (Bitmap destBitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb))
                        using (Graphics destGraphics = Graphics.FromImage(destBitmap))
                        {
                            destGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            destGraphics.DrawImage(sourceBitmap, new Rectangle(0, 0, width, height), new Rectangle(0, 0, captureRect.Width, captureRect.Height), GraphicsUnit.Pixel);
                            
                            // STEP 3: Extract raw BGR24 bytes
                            BitmapData bmpData = destBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                            System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, buffer, 0, buffer.Length);
                            destBitmap.UnlockBits(bmpData);
                        }
                    }

                    ffmpegProcess.StandardInput.BaseStream.Write(buffer, 0, buffer.Length);
                    frameCount++;
                    
                    if (frameCount % 100 == 0) Log($"Captured {frameCount} frames");
                }
                catch (Exception ex)
                {
                    Log($"Capture error: {ex.Message}");
                    break;
                }

                // STRICT TIMING: Sleep only the remaining time to hit exactly 50ms (20 FPS)
                long elapsed = sw.ElapsedMilliseconds;
                long sleepTime = targetFrameTimeMs - elapsed;
                if (sleepTime > 0)
                {
                    Thread.Sleep((int)sleepTime);
                }
            }
            
            Log($"Capture loop ended. Total frames: {frameCount}");
        }

        public void Stop()
        {
            Log("Stop() called");
            isRecording = false;
            
            if (captureThread != null) captureThread.Join(2000);
            
            if (ffmpegProcess != null && !ffmpegProcess.HasExited)
            {
                Log("Closing FFmpeg stdin to send EOF...");
                ffmpegProcess.StandardInput.Close();
                
                if (ffmpegProcess.WaitForExit(5000))
                    Log($"FFmpeg exited with code: {ffmpegProcess.ExitCode}");
                else
                {
                    Log("FFmpeg did not exit in time, killing...");
                    ffmpegProcess.Kill();
                }
            }
            
            if (File.Exists(OutputPath))
            {
                var fileInfo = new FileInfo(OutputPath);
                Log($"✅ Video saved successfully! Size: {fileInfo.Length / 1024} KB");
            }
            else
            {
                Log($"❌ Video file NOT found at: {OutputPath}");
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
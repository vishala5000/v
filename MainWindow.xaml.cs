using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace UniversalRecorder
{
    public partial class MainWindow : Window
    {
        private OverlayWindow overlay;
        private Recorder recorder;
        private WindowTracker windowTracker;
        private MouseHook mouseHook;
        
        private bool isRecording = false;
        private bool autoZoom = false;
        private bool drawingEnabled = false;
        private bool clickAnimEnabled = true;
        private bool clickSoundEnabled = true;
        
        public MainWindow()
        {
            InitializeComponent();
            AudioPlayer.Initialize();
        }
        
        private void Record_Click(object sender, RoutedEventArgs e)
        {
            if (!isRecording)
            {
                try
                {
                    string outputDir = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string outputPath = Path.Combine(outputDir, $"recording_{timestamp}.mp4");
                    
                    recorder = new Recorder(outputPath, autoZoom);
                    recorder.Start();
                    
                    windowTracker = new WindowTracker();
                    windowTracker.Start();
                    
                    mouseHook = new MouseHook();
                    mouseHook.LeftButtonDown += OnLeftDown;
                    mouseHook.LeftButtonUp += OnLeftUp;
                    mouseHook.MouseMove += OnMouseMove;
                    mouseHook.Install();
                    
                    isRecording = true;
                    RecordBtn.Content = "⏹ Stop Recording";
                    RecordBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFdc3545"));
                    StatusText.Text = "🔴 Recording...";
                    StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFff4b4b"));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Recording Error");
                }
            }
            else
            {
                StopRecording();
            }
        }
        
        private void StopRecording()
        {
            mouseHook?.Uninstall();
            windowTracker?.Stop();
            recorder?.Stop();
            
            isRecording = false;
            RecordBtn.Content = "▶ Start Recording";
            RecordBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF28a745"));
            StatusText.Text = "✅ Saved to Videos folder!";
            StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF28a745"));
            
            if (recorder != null)
            {
                MessageBox.Show($"Recording saved to:\n{recorder.OutputPath}", "Recording Complete");
            }
        }
        
        private void Zoom_Click(object sender, RoutedEventArgs e)
        {
            autoZoom = !autoZoom;
            if (autoZoom)
            {
                ZoomBtn.Content = "🔍 Auto Zoom: ON";
                ZoomBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFffc107"));
                ZoomBtn.Foreground = new SolidColorBrush(Colors.Black);
            }
            else
            {
                ZoomBtn.Content = "🔍 Auto Zoom: OFF";
                ZoomBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF444444"));
                ZoomBtn.Foreground = new SolidColorBrush(Colors.White);
            }
        }
        
        private void Draw_Click(object sender, RoutedEventArgs e)
        {
            drawingEnabled = !drawingEnabled;
            if (drawingEnabled)
            {
                if (overlay == null)
                {
                    overlay = new OverlayWindow();
                    overlay.Show();
                }
                overlay.EnableDrawing();
                DrawBtn.Content = "✏️ Drawing: ON";
                DrawBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00d2ff"));
                DrawBtn.Foreground = new SolidColorBrush(Colors.Black);
                ClearBtn.IsEnabled = true;
            }
            else
            {
                overlay?.DisableDrawing();
                DrawBtn.Content = "✏️ Drawing: OFF";
                DrawBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF444444"));
                DrawBtn.Foreground = new SolidColorBrush(Colors.White);
                ClearBtn.IsEnabled = false;
            }
        }
        
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            overlay?.ClearAll();
        }
        
        private void ClickAnim_Click(object sender, RoutedEventArgs e)
        {
            clickAnimEnabled = !clickAnimEnabled;
            if (clickAnimEnabled)
            {
                ClickAnimBtn.Content = "🎯 Click Anim: ON";
                ClickAnimBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00d2ff"));
                ClickAnimBtn.Foreground = new SolidColorBrush(Colors.Black);
            }
            else
            {
                ClickAnimBtn.Content = "🎯 Click Anim: OFF";
                ClickAnimBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF444444"));
                ClickAnimBtn.Foreground = new SolidColorBrush(Colors.White);
            }
        }
        
        private void ClickSound_Click(object sender, RoutedEventArgs e)
        {
            clickSoundEnabled = !clickSoundEnabled;
            if (clickSoundEnabled)
            {
                ClickSoundBtn.Content = "🔊 Sound: ON";
                ClickSoundBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00d2ff"));
                ClickSoundBtn.Foreground = new SolidColorBrush(Colors.Black);
            }
            else
            {
                ClickSoundBtn.Content = "🔊 Sound: OFF";
                ClickSoundBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF444444"));
                ClickSoundBtn.Foreground = new SolidColorBrush(Colors.White);
            }
        }
        
        private void OnLeftDown(int x, int y)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (clickAnimEnabled && overlay != null)
                {
                    overlay.AddRipple(x, y);
                }
                if (clickSoundEnabled)
                {
                    AudioPlayer.PlayClick();
                }
            }));
        }
        
        private void OnLeftUp(int x, int y) { }
        
        private void OnMouseMove(int x, int y) { }
        
        protected override void OnClosed(EventArgs e)
        {
            if (isRecording) StopRecording();
            overlay?.Close();
            base.OnClosed(e);
        }
    }
}
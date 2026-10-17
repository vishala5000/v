using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace UniversalRecorder
{
    public partial class MainWindow : Window
    {
        private OverlayWindow overlay;
        private Recorder recorder;
        private WindowTracker windowTracker;
        private MouseHook mouseHook;
        private bool isRecording = false, autoZoom = false, drawingEnabled = false, clickAnimEnabled = true, clickSoundEnabled = true;
        
        public MainWindow()
        {
            InitializeComponent();
            AudioPlayer.Initialize();
        }
        
        private void Minimize_Click(object sender, RoutedEventArgs e) { this.WindowState = WindowState.Minimized; }
        private void Close_Click(object sender, RoutedEventArgs e) { this.Close(); }
        
        private void Record_Click(object sender, RoutedEventArgs e)
        {
            if (!isRecording)
            {
                try
                {
                    windowTracker = new WindowTracker();
                    windowTracker.Start();
                    
                    recorder = new Recorder("", autoZoom, windowTracker);
                    recorder.Start();
                    
                    mouseHook = new MouseHook();
                    mouseHook.LeftButtonDown += OnLeftDown;
                    mouseHook.Install();
                    
                    isRecording = true;
                    RecordBtn.Content = "⏹ Stop Recording";
                    RecordBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFdc3545"));
                    StatusText.Text = "🔴 Recording... (Check Desktop for log file)";
                    StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFff4b4b"));
                }
                catch (Exception ex) 
                { 
                    MessageBox.Show($"Error: {ex.Message}", "Recording Error"); 
                    StatusText.Text = "❌ Error starting recording";
                }
            }
            else
            {
                recorder?.Stop();
                mouseHook?.Uninstall();
                windowTracker?.Stop();
                
                isRecording = false;
                RecordBtn.Content = "▶ Start Recording";
                RecordBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF28a745"));
                
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                StatusText.Text = $"✅ Saved to Desktop!";
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF28a745"));
                
                MessageBox.Show($"Recording saved to Desktop:\n{recorder.OutputPath}\n\nLog file also saved to Desktop for debugging.", "Recording Complete");
            }
        }
        
        private void Zoom_Click(object sender, RoutedEventArgs e)
        {
            autoZoom = !autoZoom;
            ZoomBtn.Content = autoZoom ? "🔍 Auto Zoom: ON" : "🔍 Auto Zoom: OFF";
            ZoomBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(autoZoom ? "#FFffc107" : "#FF444444"));
            ZoomBtn.Foreground = new SolidColorBrush(autoZoom ? Colors.Black : Colors.White);
        }
        
        private void Draw_Click(object sender, RoutedEventArgs e)
        {
            drawingEnabled = !drawingEnabled;
            if (drawingEnabled)
            {
                if (overlay == null) { overlay = new OverlayWindow(); overlay.Show(); }
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
        
        private void Clear_Click(object sender, RoutedEventArgs e) => overlay?.ClearAll();
        
        private void ClickAnim_Click(object sender, RoutedEventArgs e)
        {
            clickAnimEnabled = !clickAnimEnabled;
            ClickAnimBtn.Content = clickAnimEnabled ? "🎯 Click Anim: ON" : "🎯 Click Anim: OFF";
            ClickAnimBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(clickAnimEnabled ? "#FF00d2ff" : "#FF444444"));
            ClickAnimBtn.Foreground = new SolidColorBrush(clickAnimEnabled ? Colors.Black : Colors.White);
        }
        
        private void ClickSound_Click(object sender, RoutedEventArgs e)
        {
            clickSoundEnabled = !clickSoundEnabled;
            ClickSoundBtn.Content = clickSoundEnabled ? "🔊 Sound: ON" : "🔊 Sound: OFF";
            ClickSoundBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(clickSoundEnabled ? "#FF00d2ff" : "#FF444444"));
            ClickSoundBtn.Foreground = new SolidColorBrush(clickSoundEnabled ? Colors.Black : Colors.White);
        }
        
        private void OnLeftDown(int x, int y)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (clickAnimEnabled && overlay != null) overlay.AddRipple(x, y);
                if (clickSoundEnabled) AudioPlayer.PlayClick();
            }));
        }
        
        protected override void OnClosed(EventArgs e)
        {
            if (isRecording) Record_Click(null, null);
            overlay?.Close();
            base.OnClosed(e);
        }
    }
}
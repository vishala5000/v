using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace UniversalRecorder
{
    public partial class OverlayWindow : Window
    {
        private bool isDrawingMode = false;
        public OverlayWindow()
        {
            InitializeComponent();
            DrawCanvas.DefaultDrawingAttributes = new DrawingAttributes { Color = Colors.Red, Width = 4, Height = 4, FitToCurve = true, StylusTip = StylusTip.Ellipse };
            this.KeyDown += (s, e) => { if (e.Key == Key.F8) ToggleDrawingMode(); };
        }
        private void ToggleDraw_Click(object sender, RoutedEventArgs e) => ToggleDrawingMode();
        private void ToggleDrawingMode()
        {
            isDrawingMode = !isDrawingMode;
            if (isDrawingMode)
            {
                DrawCanvas.IsHitTestVisible = true;
                DrawCanvas.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
                ToggleDrawBtn.Content = "🛑 Stop Drawing (F8)";
                ToggleDrawBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFdc3545"));
                ToggleDrawBtn.Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                DrawCanvas.IsHitTestVisible = false;
                DrawCanvas.Background = Brushes.Transparent;
                ToggleDrawBtn.Content = "✏️ Start Drawing (F8)";
                ToggleDrawBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00d2ff"));
                ToggleDrawBtn.Foreground = new SolidColorBrush(Colors.Black);
            }
        }
        public void EnableDrawing() { this.Show(); this.Topmost = true; if (!isDrawingMode) ToggleDrawingMode(); }
        public void DisableDrawing() { if (isDrawingMode) ToggleDrawingMode(); }
        public void AddRipple(double x, double y)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                for (int i = 0; i < 3; i++)
                {
                    Ellipse ripple = new Ellipse { Stroke = new SolidColorBrush(Colors.White), StrokeThickness = 3, Width = 60, Height = 60 };
                    Canvas.SetLeft(ripple, x - 30); Canvas.SetTop(ripple, y - 30);
                    EffectsCanvas.Children.Add(ripple);
                    ripple.BeginAnimation(WidthProperty, new DoubleAnimation(60, 10, TimeSpan.FromMilliseconds(800)));
                    ripple.BeginAnimation(HeightProperty, new DoubleAnimation(60, 10, TimeSpan.FromMilliseconds(800)));
                    ripple.BeginAnimation(OpacityProperty, new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(800)));
                    DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(850) };
                    timer.Tick += (s, e) => { EffectsCanvas.Children.Remove(ripple); timer.Stop(); };
                    timer.Start();
                }
            }), DispatcherPriority.Normal);
        }
        private void DrawCanvas_MouseDown(object sender, MouseButtonEventArgs e) { }
        private void DrawCanvas_MouseMove(object sender, MouseEventArgs e) { }
        private void DrawCanvas_MouseUp(object sender, MouseButtonEventArgs e) { }
        public void ClearAll() { DrawCanvas.Strokes.Clear(); EffectsCanvas.Children.Clear(); }
        private void Clear_Click(object sender, RoutedEventArgs e) => ClearAll();
    }
}
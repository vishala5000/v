using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace UniversalRecorder
{
    public partial class OverlayWindow : Window
    {
        private bool isDrawing = false;
        private bool drawingEnabled = false;
        private Point lastPoint;
        
        public OverlayWindow()
        {
            InitializeComponent();
            
            DrawingAttributes da = new DrawingAttributes
            {
                Color = Colors.Red,
                Width = 4,
                Height = 4,
                FitToCurve = true,
                StylusTip = StylusTip.Ellipse
            };
            DrawCanvas.DefaultDrawingAttributes = da;
        }
        
        public void EnableDrawing()
        {
            drawingEnabled = true;
            DrawCanvas.EditingMode = InkCanvasEditingMode.Ink;
            DrawCanvas.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
            this.Topmost = true;
        }
        
        public void DisableDrawing()
        {
            drawingEnabled = false;
            DrawCanvas.EditingMode = InkCanvasEditingMode.None;
            DrawCanvas.Background = Brushes.Transparent;
        }
        
        public void AddRipple(double x, double y)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                for (int i = 0; i < 3; i++)
                {
                    Ellipse ripple = new Ellipse
                    {
                        Stroke = new SolidColorBrush(Colors.White),
                        StrokeThickness = 3,
                        Width = 60,
                        Height = 60
                    };
                    
                    Canvas.SetLeft(ripple, x - 30);
                    Canvas.SetTop(ripple, y - 30);
                    EffectsCanvas.Children.Add(ripple);
                    
                    DoubleAnimation widthAnim = new DoubleAnimation(60, 10, TimeSpan.FromMilliseconds(800));
                    DoubleAnimation heightAnim = new DoubleAnimation(60, 10, TimeSpan.FromMilliseconds(800));
                    DoubleAnimation opacityAnim = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(800));
                    
                    ripple.BeginAnimation(WidthProperty, widthAnim);
                    ripple.BeginAnimation(HeightProperty, heightAnim);
                    ripple.BeginAnimation(OpacityProperty, opacityAnim);
                    
                    DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(850) };
                    timer.Tick += (s, e) =>
                    {
                        EffectsCanvas.Children.Remove(ripple);
                        timer.Stop();
                    };
                    timer.Start();
                }
            }), DispatcherPriority.Normal);
        }
        
        private void DrawCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!drawingEnabled) return;
            isDrawing = true;
            lastPoint = e.GetPosition(DrawCanvas);
        }
        
        private void DrawCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDrawing || !drawingEnabled) return;
            lastPoint = e.GetPosition(DrawCanvas);
        }
        
        private void DrawCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDrawing = false;
        }
        
        public void ClearAll()
        {
            DrawCanvas.Strokes.Clear();
            EffectsCanvas.Children.Clear();
        }
    }
}
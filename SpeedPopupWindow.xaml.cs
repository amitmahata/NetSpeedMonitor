using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace NetSpeedMonitor;

public partial class SpeedPopupWindow : Window
{
    private readonly NetworkMonitor _monitor;
    private readonly List<SpeedData> _displayHistory = new();
    private bool _isClosing;
    private bool _isAnimatingOut;

    public SpeedPopupWindow(NetworkMonitor monitor)
    {
        InitializeComponent();
        _monitor = monitor;
        Opacity = 0;
    }

    /// <summary>
    /// Updates all speed displays. Called from App.xaml.cs on every monitor tick.
    /// Only updates when the popup is visible to save resources.
    /// </summary>
    public void UpdateSpeed(SpeedData data)
    {
        if (!IsVisible) return;

        // Speed values
        DownloadSpeedText.Text = SpeedFormatter.FormatSpeed(data.DownloadSpeed);
        UploadSpeedText.Text = SpeedFormatter.FormatSpeed(data.UploadSpeed);

        // Session totals
        DownloadTotalText.Text = SpeedFormatter.FormatBytes(data.TotalDownloaded);
        UploadTotalText.Text = SpeedFormatter.FormatBytes(data.TotalUploaded);
        SessionDownText.Text = SpeedFormatter.FormatBytes(data.TotalDownloaded);
        SessionUpText.Text = SpeedFormatter.FormatBytes(data.TotalUploaded);

        // Adapter
        if (!string.IsNullOrEmpty(data.AdapterName))
            AdapterText.Text = data.AdapterName;

        // Duration
        DurationText.Text = SpeedFormatter.FormatDuration(_monitor.SessionDuration);

        // History graph
        _displayHistory.Add(data);
        if (_displayHistory.Count > 60)
            _displayHistory.RemoveAt(0);

        DrawGraph();
    }

    // ─── Graph Rendering ────────────────────────────────────────────────

    private void DrawGraph()
    {
        GraphCanvas.Children.Clear();

        if (_displayHistory.Count < 2) return;

        double width = GraphCanvas.ActualWidth;
        double height = GraphCanvas.ActualHeight;
        if (width <= 0 || height <= 0) return;

        // Find max speed for scaling (minimum 1 KB for visual stability)
        long maxSpeed = Math.Max(1024,
            _displayHistory.Max(d => Math.Max(d.DownloadSpeed, d.UploadSpeed)));
        maxSpeed = (long)(maxSpeed * 1.15); // 15% headroom

        // Grid lines
        for (int i = 1; i <= 3; i++)
        {
            double y = height * i / 4;
            GraphCanvas.Children.Add(new Line
            {
                X1 = 0, Y1 = y, X2 = width, Y2 = y,
                Stroke = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 4 }
            });
        }

        // Max speed label
        var maxLabel = new TextBlock
        {
            Text = SpeedFormatter.FormatSpeed(maxSpeed),
            FontSize = 8,
            Foreground = new SolidColorBrush(Color.FromArgb(60, 150, 170, 200))
        };
        Canvas.SetLeft(maxLabel, 2);
        Canvas.SetTop(maxLabel, 2);
        GraphCanvas.Children.Add(maxLabel);

        // Draw upload first (behind), then download (in front)
        DrawSpeedLine(width, height, maxSpeed, d => d.UploadSpeed,
            Color.FromArgb(200, 255, 165, 0), Color.FromArgb(40, 255, 165, 0));
        DrawSpeedLine(width, height, maxSpeed, d => d.DownloadSpeed,
            Color.FromArgb(220, 0, 210, 255), Color.FromArgb(40, 0, 210, 255));
    }

    private void DrawSpeedLine(double width, double height, long maxSpeed,
        Func<SpeedData, long> selector, Color lineColor, Color fillColor)
    {
        if (_displayHistory.Count < 2) return;

        var points = new PointCollection();
        double step = width / 59.0; // 60 data points across

        for (int i = 0; i < _displayHistory.Count; i++)
        {
            double x = (60 - _displayHistory.Count + i) * step;
            double value = selector(_displayHistory[i]);
            double y = height - (value / (double)maxSpeed * (height - 4)); // 4px top padding
            y = Math.Clamp(y, 2, height);
            points.Add(new Point(x, y));
        }

        // Smooth line
        var polyline = new Polyline
        {
            Points = points,
            Stroke = new SolidColorBrush(lineColor),
            StrokeThickness = 2,
            StrokeLineJoin = PenLineJoin.Round
        };
        GraphCanvas.Children.Add(polyline);

        // Gradient fill beneath the line
        var fillPoints = new PointCollection(points);
        fillPoints.Add(new Point(points[points.Count - 1].X, height));
        fillPoints.Add(new Point(points[0].X, height));

        var polygon = new Polygon
        {
            Points = fillPoints,
            Fill = new LinearGradientBrush(
                fillColor,
                Color.FromArgb(3, fillColor.R, fillColor.G, fillColor.B),
                90)
        };
        GraphCanvas.Children.Add(polygon);
    }

    // ─── Show / Hide with Animation ─────────────────────────────────────

    public void ShowPopup(double? widgetLeft = null, double? widgetWidth = null)
    {
        _isAnimatingOut = false;

        // Load history from monitor
        var history = _monitor.History;
        _displayHistory.Clear();
        _displayHistory.AddRange(history);

        // Position above the taskbar
        var workingArea = SystemParameters.WorkArea;
        
        if (widgetLeft.HasValue && widgetWidth.HasValue)
        {
            // Center horizontally above the floating widget
            Left = widgetLeft.Value + (widgetWidth.Value - Width) / 2;
            
            // Bounds check
            if (Left + Width > workingArea.Right)
                Left = workingArea.Right - Width - 8;
            if (Left < workingArea.Left)
                Left = workingArea.Left + 8;
        }
        else
        {
            Left = workingArea.Right - Width - 8;
        }
        
        Top = workingArea.Bottom - Height - 8;

        Show();
        Activate();

        // Show current data
        if (_displayHistory.Count > 0)
        {
            var latest = _displayHistory[^1];
            DownloadSpeedText.Text = SpeedFormatter.FormatSpeed(latest.DownloadSpeed);
            UploadSpeedText.Text = SpeedFormatter.FormatSpeed(latest.UploadSpeed);
            DownloadTotalText.Text = SpeedFormatter.FormatBytes(latest.TotalDownloaded);
            UploadTotalText.Text = SpeedFormatter.FormatBytes(latest.TotalUploaded);
            SessionDownText.Text = SpeedFormatter.FormatBytes(latest.TotalDownloaded);
            SessionUpText.Text = SpeedFormatter.FormatBytes(latest.TotalUploaded);
            if (!string.IsNullOrEmpty(latest.AdapterName))
                AdapterText.Text = latest.AdapterName;
            DurationText.Text = SpeedFormatter.FormatDuration(_monitor.SessionDuration);
            DrawGraph();
        }

        // Fade in + slide up
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(OpacityProperty, fadeIn);

        SlideTransform.Y = 12;
        var slideUp = new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        SlideTransform.BeginAnimation(TranslateTransform.YProperty, slideUp);
    }

    public void HidePopup()
    {
        if (!IsVisible || _isAnimatingOut) return;
        _isAnimatingOut = true;

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(160));
        fadeOut.Completed += (_, _) =>
        {
            if (_isAnimatingOut)
            {
                Hide();
                _isAnimatingOut = false;
            }
        };
        BeginAnimation(OpacityProperty, fadeOut);
    }

    public void ForceClose()
    {
        _isClosing = true;
        Close();
    }

    // ─── Window Events ──────────────────────────────────────────────────

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        if (!_isClosing && IsVisible)
            HidePopup();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isClosing)
        {
            e.Cancel = true;
            HidePopup();
        }
        else
        {
            base.OnClosing(e);
        }
    }
}

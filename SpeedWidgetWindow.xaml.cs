using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace NetSpeedMonitor;

public partial class SpeedWidgetWindow : Window
{
    private readonly App _app;
    private readonly System.Windows.Threading.DispatcherTimer _positionTimer;

    // --- Win32 APIs for finding the System Tray rect and parenting ---

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string? windowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const int WS_POPUP = unchecked((int)0x80000000);
    private const int WS_CHILD = 0x40000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;

    public SpeedWidgetWindow(App app)
    {
        InitializeComponent();
        _app = app;

        this.Loaded += SpeedWidgetWindow_Loaded;

        // Snapping position verifier timer (runs once every 500ms to dynamically adapt to taskbar shifts/DPI scaling)
        _positionTimer = new System.Windows.Threading.DispatcherTimer();
        _positionTimer.Interval = TimeSpan.FromMilliseconds(500);
        _positionTimer.Tick += PositionTimer_Tick;
        _positionTimer.Start();
    }

    private void SpeedWidgetWindow_Loaded(object sender, RoutedEventArgs e)
    {
        PositionWidgetBesideTray();
    }

    private void PositionTimer_Tick(object? sender, EventArgs e)
    {
        PositionWidgetBesideTray();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        try
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            IntPtr hwnd = helper.Handle;

            IntPtr shellTray = FindWindow("Shell_TrayWnd", null);
            if (shellTray != IntPtr.Zero)
            {
                // 1. Convert Top-Level Window (WS_POPUP) to Child Window (WS_CHILD)
                int style = GetWindowLong(hwnd, GWL_STYLE);
                SetWindowLong(hwnd, GWL_STYLE, (style & ~WS_POPUP) | WS_CHILD);

                // 2. Set ToolWindow and NoActivate styles
                int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);

                // 3. Parent the window natively to the Windows Taskbar!
                // This makes the widget a native child of the taskbar:
                // - It will stay sticked to the taskbar.
                // - It naturally hides during fullscreen YouTube videos (since Windows hides the taskbar parent).
                // - It natively slides down/up when taskbar autohide is active.
                // - It stays behind standard windows if they are dragged on top of the taskbar.
                SetParent(hwnd, shellTray);
            }
        }
        catch
        {
            // Fail-safe
        }
    }

    /// <summary>
    /// Locates the tray notification area (TrayNotifyWnd) and positions the widget 
    /// exactly beside the overflow arrow, relative to the taskbar parent's client area.
    /// </summary>
    public void PositionWidgetBesideTray()
    {
        try
        {
            IntPtr shellTray = FindWindow("Shell_TrayWnd", null);
            if (shellTray != IntPtr.Zero)
            {
                IntPtr trayNotify = FindWindowEx(shellTray, IntPtr.Zero, "TrayNotifyWnd", null);
                if (trayNotify != IntPtr.Zero)
                {
                    if (GetWindowRect(shellTray, out RECT trayRect) && GetWindowRect(trayNotify, out RECT notifyRect))
                    {
                        // Calculate DPI Scale
                        double dpiScaleX = 1.0;
                        double dpiScaleY = 1.0;
                        var source = PresentationSource.FromVisual(this);
                        if (source?.CompositionTarget != null)
                        {
                            dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                            dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
                        }

                        // Compute dimensions in physical pixels
                        int widgetWidthPixels = (int)(Width * dpiScaleX);
                        int widgetHeightPixels = (int)(Height * dpiScaleY);

                        // Relative Left of notify tray area inside parent taskbar
                        int relativeLeft = (notifyRect.Left - trayRect.Left) - widgetWidthPixels - 10;

                        // Center vertically inside the taskbar
                        int trayHeight = trayRect.Bottom - trayRect.Top;
                        int relativeTop = (trayHeight - widgetHeightPixels) / 2;

                        // Position child window exactly using Win32 SetWindowPos
                        var helper = new System.Windows.Interop.WindowInteropHelper(this);
                        IntPtr hwnd = helper.Handle;
                        if (hwnd != IntPtr.Zero)
                        {
                            SetWindowPos(hwnd, IntPtr.Zero, relativeLeft, relativeTop, widgetWidthPixels, widgetHeightPixels, SWP_NOZORDER | SWP_NOACTIVATE);
                        }
                    }
                }
            }
        }
        catch
        {
            // Fail-safe
        }
    }

    public void UpdateSpeed(long downloadSpeed, long uploadSpeed)
    {
        DownloadText.Text = SpeedFormatter.FormatSpeed(downloadSpeed);
        UploadText.Text = SpeedFormatter.FormatSpeed(uploadSpeed);
    }

    private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            _app.ShowPopupFromWidget();
        }
    }

    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonUp(e);

        var menu = new System.Windows.Controls.ContextMenu();

        var showDetailsItem = new System.Windows.Controls.MenuItem { Header = "📊 Show Details" };
        showDetailsItem.Click += (s, ev) => _app.ShowPopupFromWidget();
        menu.Items.Add(showDetailsItem);

        // --- Speed Unit Submenu ---
        var unitMenuItem = new System.Windows.Controls.MenuItem { Header = "⚙ Speed Unit" };
        var units = new[]
        {
            (SpeedUnit.Auto, "Auto (Dynamic)"),
            (SpeedUnit.Bps, "B/s"),
            (SpeedUnit.KBps, "KB/s"),
            (SpeedUnit.Kbps, "Kb/s"),
            (SpeedUnit.Mbps, "Mb/s")
        };

        foreach (var (unit, label) in units)
        {
            var item = new System.Windows.Controls.MenuItem
            {
                Header = label,
                IsCheckable = true,
                IsChecked = SpeedFormatter.ActiveUnit == unit
            };
            item.Click += (s, ev) => _app.SetSpeedUnit(unit);
            unitMenuItem.Items.Add(item);
        }
        menu.Items.Add(unitMenuItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var hideWidgetItem = new System.Windows.Controls.MenuItem { Header = "✕ Hide Widget" };
        hideWidgetItem.Click += (s, ev) => Hide();
        menu.Items.Add(hideWidgetItem);

        var exitItem = new System.Windows.Controls.MenuItem { Header = "✕ Exit Application" };
        exitItem.Click += (s, ev) => _app.ExitApplication();
        menu.Items.Add(exitItem);

        this.ContextMenu = menu;
        this.ContextMenu.IsOpen = true;
    }

    /// <summary>
    /// Gets the actual screen position and width of the widget in WPF logical pixels.
    /// This works perfectly even though the widget is a native child window.
    /// </summary>
    public (double Left, double Width) GetScreenPosition()
    {
        try
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            IntPtr hwnd = helper.Handle;
            if (hwnd != IntPtr.Zero && GetWindowRect(hwnd, out RECT rect))
            {
                double dpiScaleX = 1.0;
                var source = PresentationSource.FromVisual(this);
                if (source?.CompositionTarget != null)
                {
                    dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                }
                
                double left = rect.Left / dpiScaleX;
                double width = (rect.Right - rect.Left) / dpiScaleX;
                return (left, width);
            }
        }
        catch
        {
            // Fail-safe
        }
        return (Left, Width);
    }
}

using System.Runtime.InteropServices;
using System.Windows;

namespace NetSpeedMonitor;

public partial class App : Application
{
    private NetworkMonitor _monitor = null!;
    private SpeedPopupWindow _popup = null!;
    private SpeedWidgetWindow _widget = null!;
    private SpeedData? _lastData;
    private static readonly Mutex _mutex = new(true, "{NetSpeedMonitor-8A3F-4B2E-9D1C-7E6F5A4B3C2D}");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Single-instance enforcement
        if (!_mutex.WaitOne(TimeSpan.Zero, true))
        {
            MessageBox.Show("Net Speed Monitor is already running.",
                "Net Speed Monitor", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        _monitor = new NetworkMonitor();
        _popup = new SpeedPopupWindow(_monitor);
        _widget = new SpeedWidgetWindow(this);

        _monitor.SpeedUpdated += OnSpeedUpdated;
        _monitor.Start();

        _widget.Show();
    }

    private void OnSpeedUpdated(SpeedData data)
    {
        _lastData = data;
        Dispatcher.Invoke(() =>
        {
            _popup.UpdateSpeed(data);
            if (_widget.IsVisible)
            {
                _widget.UpdateSpeed(data.DownloadSpeed, data.UploadSpeed);
            }
        });
    }

    private void TogglePopup()
    {
        if (_popup.IsVisible)
            _popup.HidePopup();
        else
            ShowPopup();
    }

    private void ShowPopup()
    {
        var (left, width) = _widget.GetScreenPosition();
        _popup.ShowPopup(left, width);
    }

    public void ShowPopupFromWidget()
    {
        ShowPopup();
    }

    public void SetSpeedUnit(SpeedUnit unit)
    {
        SpeedFormatter.ActiveUnit = unit;
        Dispatcher.Invoke(() =>
        {
            if (_lastData != null)
            {
                OnSpeedUpdated(_lastData);
            }
        });
    }

    public void ExitApplication()
    {
        _monitor.Stop();
        _monitor.Dispose();
        _popup.ForceClose();
        _widget.Close();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { _mutex.ReleaseMutex(); } catch { }
        base.OnExit(e);
    }
}

using System.Net.NetworkInformation;
using System.Timers;

namespace NetSpeedMonitor;

/// <summary>
/// Data class holding current network speed information.
/// </summary>
public class SpeedData
{
    public long DownloadSpeed { get; set; }   // bytes per second
    public long UploadSpeed { get; set; }     // bytes per second
    public long TotalDownloaded { get; set; } // cumulative session bytes
    public long TotalUploaded { get; set; }   // cumulative session bytes
    public string AdapterName { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Monitors network interfaces and calculates real-time upload/download speeds.
/// Polls every 1 second and aggregates traffic across all active adapters.
/// </summary>
public class NetworkMonitor : IDisposable
{
    private readonly System.Timers.Timer _timer;
    private Dictionary<string, (long sent, long received)> _previousValues = new();
    private long _sessionDownloaded;
    private long _sessionUploaded;
    private readonly List<SpeedData> _history = new();
    private readonly object _lock = new();
    private bool _isFirstTick = true;
    private readonly DateTime _sessionStart;

    /// <summary>Fired on every speed update (every ~1 second).</summary>
    public event Action<SpeedData>? SpeedUpdated;

    /// <summary>Rolling history of the last 60 speed samples.</summary>
    public IReadOnlyList<SpeedData> History
    {
        get { lock (_lock) return _history.ToList(); }
    }

    /// <summary>How long the monitor has been running.</summary>
    public TimeSpan SessionDuration => DateTime.Now - _sessionStart;

    public NetworkMonitor()
    {
        _sessionStart = DateTime.Now;
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += OnTimerElapsed;
    }

    public void Start()
    {
        _isFirstTick = true;
        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up
                          && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback
                          && ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .ToList();

            long totalDown = 0, totalUp = 0;
            string adapterName = "";

            foreach (var nic in interfaces)
            {
                try
                {
                    var stats = nic.GetIPv4Statistics();

                    if (_previousValues.TryGetValue(nic.Id, out var prev))
                    {
                        var down = stats.BytesReceived - prev.received;
                        var up = stats.BytesSent - prev.sent;
                        // Guard against counter resets (adapter reconnect)
                        if (down >= 0) totalDown += down;
                        if (up >= 0) totalUp += up;
                    }

                    _previousValues[nic.Id] = (stats.BytesSent, stats.BytesReceived);

                    if (string.IsNullOrEmpty(adapterName))
                        adapterName = $"{nic.Name} ({nic.NetworkInterfaceType})";
                }
                catch
                {
                    // Interface may disappear mid-enumeration
                }
            }

            // Skip the first tick — we need two data points to compute a delta
            if (_isFirstTick)
            {
                _isFirstTick = false;
                return;
            }

            _sessionDownloaded += totalDown;
            _sessionUploaded += totalUp;

            var data = new SpeedData
            {
                DownloadSpeed = totalDown,
                UploadSpeed = totalUp,
                TotalDownloaded = _sessionDownloaded,
                TotalUploaded = _sessionUploaded,
                AdapterName = adapterName,
                Timestamp = DateTime.Now
            };

            lock (_lock)
            {
                _history.Add(data);
                if (_history.Count > 60)
                    _history.RemoveAt(0);
            }

            SpeedUpdated?.Invoke(data);
        }
        catch
        {
            // Swallow unexpected errors to keep the timer alive
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
    }
}

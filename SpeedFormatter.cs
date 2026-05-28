namespace NetSpeedMonitor;

public enum SpeedUnit
{
    Auto,
    Bps,
    KBps,
    Kbps, // Displays as "Kb/s"
    Mbps  // Displays as "Mb/s"
}

public static class SpeedFormatter
{
    public static SpeedUnit ActiveUnit { get; set; } = SpeedUnit.Auto;

    /// <summary>Formats bytes/sec into a full display string according to the active unit preference.</summary>
    public static string FormatSpeed(long bytesPerSecond)
    {
        double speed = bytesPerSecond;
        switch (ActiveUnit)
        {
            case SpeedUnit.Bps:
                return $"{speed:F0} B/s";
            case SpeedUnit.KBps:
                return $"{speed / 1024.0:F1} KB/s";
            case SpeedUnit.Kbps:
                return $"{(speed * 8.0) / 1000.0:F1} Kb/s";
            case SpeedUnit.Mbps:
                return $"{(speed * 8.0) / 1_000_000.0:F1} Mb/s";
            case SpeedUnit.Auto:
            default:
                // Auto (Dynamic) now formats to bits (Gb/s, Mb/s, Kb/s, b/s) with a small 'b' for extreme familiarity!
                double bits = speed * 8.0;
                if (bits >= 1_000_000_000) return $"{bits / 1_000_000_000.0:F1} Gb/s";
                if (bits >= 1_000_000)     return $"{bits / 1_000_000.0:F1} Mb/s";
                if (bits >= 1000)          return $"{bits / 1000.0:F1} Kb/s";
                return $"{bits:F0} b/s";
        }
    }

    /// <summary>Formats a byte count into a readable bit-based string (e.g. "1.25 Mb" or "Gb").</summary>
    public static string FormatBytes(long bytes)
    {
        // Converts byte totals to bit totals for perfect consistency with Megabits (Mb) preference!
        double bits = bytes * 8.0;
        if (bits >= 1_000_000_000_000) return $"{bits / 1_000_000_000_000.0:F2} Tb";
        if (bits >= 1_000_000_000)     return $"{bits / 1_000_000_000.0:F2} Gb";
        if (bits >= 1_000_000)         return $"{bits / 1_000_000.0:F1} Mb";
        if (bits >= 1000)              return $"{bits / 1000.0:F1} Kb";
        return $"{bits:F0} b";
    }

    /// <summary>
    /// Ultra-compact format for the 16x16 tray icon, respecting preferences as much as possible in 3 characters.
    /// </summary>
    public static string FormatSpeedCompact(long bytesPerSecond)
    {
        double speed = bytesPerSecond;
        switch (ActiveUnit)
        {
            case SpeedUnit.Bps:
                return $"{speed:F0}";
            case SpeedUnit.KBps:
                return $"{speed / 1024.0:F0}K";
            case SpeedUnit.Kbps:
                return $"{(speed * 8.0) / 1000.0:F0}k";
            case SpeedUnit.Mbps:
                return $"{(speed * 8.0) / 1_000_000.0:F1}m";
            case SpeedUnit.Auto:
            default:
                double bits = speed * 8.0;
                if (bits >= 1_000_000_000) return $"{bits / 1_000_000_000.0:F0}g";
                if (bits >= 100_000_000)   return $"{bits / 1_000_000.0:F0}m";
                if (bits >= 1_000_000)     return $"{bits / 1_000_000.0:F1}m";
                if (bits >= 100_000)       return $"{bits / 1000.0:F0}k";
                if (bits >= 1000)          return $"{bits / 1000.0:F0}k";
                return "0k";
        }
    }

    /// <summary>Formats a TimeSpan into a compact duration string (e.g. "2h 15m").</summary>
    public static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        if (duration.TotalMinutes >= 1)
            return $"{(int)duration.TotalMinutes}m {duration.Seconds}s";
        return $"{duration.Seconds}s";
    }
}

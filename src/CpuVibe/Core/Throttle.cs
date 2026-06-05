using System.Diagnostics;

/// <summary>
/// Throttles CPU execution to match real-world clock speed.
/// Tracks cycles executed vs real elapsed time and sleeps when ahead.
/// </summary>
public class Throttle
{
    private readonly Stopwatch _stopwatch = new();
    private long _cyclesAtLastCheck;
    private double _cyclesPerMs;

    /// <summary>
    /// Target clock speed in Hz (cycles per second).
    /// </summary>
    public long TargetHz { get; set; }

    /// <summary>
    /// Total cycles executed since last Reset().
    /// </summary>
    public long TotalCycles { get; private set; }

    /// <summary>
    /// How many cycles to accumulate before checking time.
    /// Default 10000 — reduces Sleep overhead.
    /// </summary>
    public long CheckInterval { get; set; } = 10_000;

    /// <summary>
    /// Maximum ms to sleep per check. Prevents huge sleeps on first catch-up.
    /// </summary>
    public int MaxSleepMs { get; set; } = 10;

    public Throttle(long targetHz)
    {
        TargetHz = targetHz;
        _cyclesPerMs = targetHz / 1000.0;
    }

    /// <summary>
    /// Call after each instruction. Pass the cycles that instruction consumed.
    /// Will sleep if emulator is running ahead of real time.
    /// </summary>
    public void AddCycles(byte cycles)
    {
        TotalCycles += cycles;
        _cyclesAtLastCheck += cycles;

        if (_cyclesAtLastCheck >= CheckInterval)
        {
            ThrottleIfNeeded();
            _cyclesAtLastCheck = 0;
        }
    }

    private void ThrottleIfNeeded()
    {
        if (!_stopwatch.IsRunning)
        {
            _stopwatch.Start();
            return;
        }

        double expectedMs = TotalCycles / _cyclesPerMs;
        double actualMs = _stopwatch.Elapsed.TotalMilliseconds;
        double diff = expectedMs - actualMs;

        if (diff > 1.0)
        {
            int sleepMs = Math.Min((int)diff, MaxSleepMs);
            Thread.Sleep(sleepMs);
        }
    }

    public void Reset()
    {
        _stopwatch.Restart();
        TotalCycles = 0;
        _cyclesAtLastCheck = 0;
    }

    /// <summary>
    /// Returns effective MIPS (millions of instructions per second) relative to target.
    /// 1.0 = running at exact real-time speed.
    /// </summary>
    public double GetSpeedRatio()
    {
        if (!_stopwatch.IsRunning || _stopwatch.Elapsed.TotalSeconds < 0.1)
            return 0;

        double actualCps = TotalCycles / _stopwatch.Elapsed.TotalSeconds;
        return actualCps / TargetHz;
    }

    /// <summary>
    /// Returns actual cycles per second.
    /// </summary>
    public double GetActualCps()
    {
        if (!_stopwatch.IsRunning || _stopwatch.Elapsed.TotalSeconds < 0.1)
            return 0;
        return TotalCycles / _stopwatch.Elapsed.TotalSeconds;
    }
}

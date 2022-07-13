using System;
using System.Threading;
using System.Threading.Tasks;

namespace Serilog.Sinks.GrafanaLoki.Common;

public class PortableTimer : IDisposable
{
    private readonly object _syncRoot = new();
    private readonly Func<Task> _onTick;
    private readonly Timer _timer;

    private bool _running;
    private bool _disposed;

    public PortableTimer(Func<Task> onTick)
    {
        _onTick = onTick ?? throw new ArgumentNullException(nameof(onTick));
        _timer = new Timer(_ => OnTick(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start(TimeSpan interval)
    {
        if (interval < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));

        lock (_syncRoot)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PortableTimer));
            }

            _timer.Change(interval, Timeout.InfiniteTimeSpan);
        }
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            if (_disposed)
            {
                return;
            }

            while (_running)
            {
                Monitor.Wait(_syncRoot);
            }

            _timer.Dispose();

            _disposed = true;
        }
    }

    private async void OnTick()
    {
        try
        {
            lock (_syncRoot)
            {
                if (_disposed)
                {
                    return;
                }

                // There's a little bit of raciness here, but it's needed to support the
                // current API, which allows the tick handler to reenter and set the next interval.

                if (_running)
                {
                    Monitor.Wait(_syncRoot);

                    if (_disposed)
                    {
                        return;
                    }
                }

                _running = true;
            }

            await _onTick();
        }
        finally
        {
            lock (_syncRoot)
            {
                _running = false;
                Monitor.PulseAll(_syncRoot);
            }
        }
    }
}
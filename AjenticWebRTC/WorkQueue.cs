using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AjenticWebRTC;

/// <summary>Serial work queue that marshals callbacks off native threads onto a single managed thread.</summary>
public sealed class WorkQueue : IDisposable
{
    private readonly Channel<Action> _channel;
    private readonly Thread _worker;
    private readonly CancellationTokenSource _cts = new();
    private int _disposed;

    /// <summary>Creates a new work queue and starts its background worker thread.</summary>
    public WorkQueue()
    {
        _channel = Channel.CreateUnbounded<Action>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });
        _worker = new Thread(Run)
        {
            IsBackground = true,
            Name = "WebRTC-WorkQueue",
        };
        _worker.Start();
    }

    /// <summary>Schedules an action to run on the queue's worker thread.</summary>
    /// <exception cref="ObjectDisposedException">The queue has been disposed.</exception>
    public void Enqueue(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (Volatile.Read(ref _disposed) != 0)
            throw new ObjectDisposedException(nameof(WorkQueue));
        if (!_channel.Writer.TryWrite(action))
            throw new ObjectDisposedException(nameof(WorkQueue));
    }

    private void Run()
    {
        var reader = _channel.Reader;
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                var readTask = reader.WaitToReadAsync(_cts.Token).AsTask();
                readTask.Wait();
                if (!readTask.Result) break;
                while (reader.TryRead(out var action))
                {
                    try { action(); }
                    catch { /* swallow to keep the pump alive */ }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (AggregateException ae) when (ae.InnerException is OperationCanceledException) { }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _channel.Writer.TryComplete();
        _cts.Cancel();
        try { _worker.Join(TimeSpan.FromSeconds(2)); } catch { }
        _cts.Dispose();
    }
}

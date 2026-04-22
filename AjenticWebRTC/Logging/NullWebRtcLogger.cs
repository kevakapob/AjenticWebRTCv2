using System;
using Microsoft.Extensions.Logging;

namespace AjenticWebRTC.Logging;

/// <summary>No-op <see cref="ILogger"/> used when no logger is supplied.</summary>
public sealed class NullWebRtcLogger : ILogger
{
    /// <summary>Shared singleton instance.</summary>
    public static readonly NullWebRtcLogger Instance = new();

    private NullWebRtcLogger() { }

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => false;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}

/// <summary>Generic wrapper that makes <see cref="NullWebRtcLogger"/> satisfy <see cref="ILogger{T}"/>.</summary>
public sealed class NullWebRtcLogger<T> : ILogger<T>
{
    /// <summary>Shared singleton.</summary>
    public static readonly NullWebRtcLogger<T> Instance = new();

    private NullWebRtcLogger() { }

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => false;
    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}

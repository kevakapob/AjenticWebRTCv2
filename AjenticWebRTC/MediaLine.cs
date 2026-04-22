using System;
using System.Collections.Generic;
using System.Linq;
using AjenticWebRTC.Interop;
using AjenticWebRTC.Logging;
using Microsoft.Extensions.Logging;

namespace AjenticWebRTC;

/// <summary>Collection of remote media tracks belonging to a peer connection.</summary>
public sealed class MediaLine : IDisposable
{
    private readonly Dictionary<IntPtr, RemoteVideoTrack> _videoTracks = new();
    private readonly Dictionary<IntPtr, object> _audioTracks = new();
    private readonly WorkQueue _workQueue;
    private readonly ILogger _logger;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>Raised after a new remote video track is added.</summary>
    public event EventHandler<RemoteVideoTrack>? VideoTrackAdded;
    /// <summary>Raised after a remote video track is removed.</summary>
    public event EventHandler<RemoteVideoTrack>? VideoTrackRemoved;

    /// <summary>Current snapshot of remote video tracks.</summary>
    public IReadOnlyList<RemoteVideoTrack> VideoTracks
    {
        get { lock (_lock) return _videoTracks.Values.ToList(); }
    }

    internal MediaLine(WorkQueue workQueue, ILogger? logger)
    {
        _workQueue = workQueue ?? throw new ArgumentNullException(nameof(workQueue));
        _logger = logger ?? NullWebRtcLogger.Instance;
    }

    /// <summary>Registers a newly added native track with this media line.</summary>
    public void AddTrack(TrackKind kind, IntPtr handle)
    {
        if (handle == IntPtr.Zero) return;
        RemoteVideoTrack? added = null;
        lock (_lock)
        {
            if (_disposed) return;
            if (kind == TrackKind.Video)
            {
                if (_videoTracks.ContainsKey(handle)) return;
                added = new RemoteVideoTrack(handle, _logger, _workQueue);
                _videoTracks[handle] = added;
            }
            else
            {
                _audioTracks[handle] = new object();
            }
        }
        if (added != null)
        {
            _logger.LogInformation("MediaLine added video track {Handle}", handle);
            VideoTrackAdded?.Invoke(this, added);
        }
    }

    /// <summary>Removes and disposes a track that was closed on the native side.</summary>
    public void RemoveTrack(TrackKind kind, IntPtr handle)
    {
        RemoteVideoTrack? removed = null;
        lock (_lock)
        {
            if (kind == TrackKind.Video)
            {
                if (_videoTracks.Remove(handle, out removed)) { /* removed */ }
            }
            else
            {
                _audioTracks.Remove(handle);
            }
        }
        if (removed != null)
        {
            _logger.LogInformation("MediaLine removed video track {Handle}", handle);
            VideoTrackRemoved?.Invoke(this, removed);
            removed.Dispose();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        List<RemoteVideoTrack> toDispose;
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            toDispose = _videoTracks.Values.ToList();
            _videoTracks.Clear();
            _audioTracks.Clear();
        }
        foreach (var t in toDispose) t.Dispose();
    }
}

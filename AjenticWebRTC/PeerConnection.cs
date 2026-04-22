using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AjenticWebRTC.Exceptions;
using AjenticWebRTC.Interop;
using AjenticWebRTC.Logging;
using Microsoft.Extensions.Logging;

namespace AjenticWebRTC;

/// <summary>Configuration passed to <see cref="PeerConnection"/> at construction.</summary>
public sealed class PeerConnectionConfiguration
{
    /// <summary>ICE servers as a semicolon-separated list of URLs.</summary>
    public string IceServers { get; set; } = string.Empty;
    /// <summary>ICE transport type (0 = all, 1 = relay).</summary>
    public int IceTransportType { get; set; } = 0;
    /// <summary>Bundle policy (0 = balanced, 1 = max-bundle, 2 = max-compat).</summary>
    public int BundlePolicy { get; set; } = 0;
    /// <summary>SDP semantic (0 = unified-plan, 1 = plan-b).</summary>
    public int SdpSemantic { get; set; } = 0;
}

/// <summary>Event arguments for a local ICE candidate ready to signal.</summary>
public sealed class IceCandidateReadyEventArgs : EventArgs
{
    /// <summary>The candidate string.</summary>
    public string Candidate { get; }
    /// <summary>m-line index.</summary>
    public int SdpMlineIndex { get; }
    /// <summary>SDP mid identifier.</summary>
    public string SdpMid { get; }
    /// <summary>Creates a new instance.</summary>
    public IceCandidateReadyEventArgs(string candidate, int sdpMlineIndex, string sdpMid)
    {
        Candidate = candidate;
        SdpMlineIndex = sdpMlineIndex;
        SdpMid = sdpMid;
    }
}

/// <summary>Event arguments for a local SDP (offer or answer) ready to signal.</summary>
public sealed class LocalSdpReadyEventArgs : EventArgs
{
    /// <summary>"offer" or "answer".</summary>
    public string Type { get; }
    /// <summary>SDP text.</summary>
    public string Sdp { get; }
    /// <summary>Creates a new instance.</summary>
    public LocalSdpReadyEventArgs(string type, string sdp)
    {
        Type = type;
        Sdp = sdp;
    }
}

/// <summary>Managed wrapper around a native mrwebrtc peer connection.</summary>
public sealed class PeerConnection : IDisposable
{
    private readonly ILogger _logger;
    private readonly PeerConnectionHandle _handle;
    private readonly WorkQueue _workQueue;
    private readonly MediaLine _mediaLine;

    private readonly PeerConnectionConnectedCallback _connectedCb;
    private readonly PeerConnectionDisconnectedCallback _disconnectedCb;
    private readonly PeerConnectionIceCandidateReadyCallback _iceCb;
    private readonly PeerConnectionLocalSdpReadyCallback _sdpCb;
    private readonly PeerConnectionTrackAddedCallback _trackAddedCb;
    private readonly PeerConnectionTrackRemovedCallback _trackRemovedCb;

    private GCHandle _connectedHandle, _disconnectedHandle, _iceHandle, _sdpHandle, _trackAddedHandle, _trackRemovedHandle;

    private TaskCompletionSource<string>? _pendingSdpTcs;
    private readonly object _sdpLock = new();
    private int _disposed;

    /// <summary>Raised when the peer connection enters the connected state.</summary>
    public event EventHandler? Connected;
    /// <summary>Raised when the peer connection is disconnected.</summary>
    public event EventHandler? Disconnected;
    /// <summary>Raised when a local ICE candidate is ready to send to the remote peer.</summary>
    public event EventHandler<IceCandidateReadyEventArgs>? IceCandidateReady;
    /// <summary>Raised when a local SDP (offer or answer) is ready to send to the remote peer.</summary>
    public event EventHandler<LocalSdpReadyEventArgs>? LocalSdpReady;

    /// <summary>Exposes the set of remote media tracks attached to this connection.</summary>
    public MediaLine MediaLine => _mediaLine;

    /// <summary>Creates and initializes a new peer connection.</summary>
    public PeerConnection(PeerConnectionConfiguration config, ILogger<PeerConnection>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        _logger = (ILogger?)logger ?? NullWebRtcLogger.Instance;
        _workQueue = new WorkQueue();
        _mediaLine = new MediaLine(_workQueue, _logger);

        var native = new MrsPeerConnectionConfiguration
        {
            IceServers = config.IceServers,
            IceTransportType = config.IceTransportType,
            BundlePolicy = config.BundlePolicy,
            SdpSemantic = config.SdpSemantic,
        };

        int rc;
        IntPtr raw;
        try
        {
            rc = NativeMethods.mrsPeerConnectionCreate(ref native, out raw);
        }
        catch (Exception ex)
        {
            _workQueue.Dispose();
            throw new WebRtcInitializationException("Failed to call mrsPeerConnectionCreate.", ex);
        }

        if (rc != (int)MrsResult.Success || raw == IntPtr.Zero)
        {
            _workQueue.Dispose();
            throw new WebRtcInitializationException($"mrsPeerConnectionCreate returned {rc}.");
        }

        _handle = new PeerConnectionHandle(raw, ownsHandle: true);

        _connectedCb = _ => _workQueue.Enqueue(() => Connected?.Invoke(this, EventArgs.Empty));
        _disconnectedCb = _ => _workQueue.Enqueue(() => Disconnected?.Invoke(this, EventArgs.Empty));
        _iceCb = (IntPtr _, string candidate, int idx, string mid) =>
            _workQueue.Enqueue(() => IceCandidateReady?.Invoke(this, new IceCandidateReadyEventArgs(candidate, idx, mid)));
        _sdpCb = (IntPtr _, string type, string sdp) =>
        {
            TaskCompletionSource<string>? tcs;
            lock (_sdpLock)
            {
                tcs = _pendingSdpTcs;
                _pendingSdpTcs = null;
            }
            _workQueue.Enqueue(() => LocalSdpReady?.Invoke(this, new LocalSdpReadyEventArgs(type, sdp)));
            tcs?.TrySetResult(sdp);
        };
        _trackAddedCb = (IntPtr _, TrackKind kind, IntPtr h) => _workQueue.Enqueue(() => _mediaLine.AddTrack(kind, h));
        _trackRemovedCb = (IntPtr _, TrackKind kind, IntPtr h) => _workQueue.Enqueue(() => _mediaLine.RemoveTrack(kind, h));

        _connectedHandle = GCHandle.Alloc(_connectedCb);
        _disconnectedHandle = GCHandle.Alloc(_disconnectedCb);
        _iceHandle = GCHandle.Alloc(_iceCb);
        _sdpHandle = GCHandle.Alloc(_sdpCb);
        _trackAddedHandle = GCHandle.Alloc(_trackAddedCb);
        _trackRemovedHandle = GCHandle.Alloc(_trackRemovedCb);

        var h = _handle.DangerousGetHandle();
        NativeMethods.mrsPeerConnectionRegisterConnectedCallback(h, _connectedCb, IntPtr.Zero);
        NativeMethods.mrsPeerConnectionRegisterDisconnectedCallback(h, _disconnectedCb, IntPtr.Zero);
        NativeMethods.mrsPeerConnectionRegisterIceCandidateReadytoSendCallback(h, _iceCb, IntPtr.Zero);
        NativeMethods.mrsPeerConnectionRegisterLocalSdpReadytoSendCallback(h, _sdpCb, IntPtr.Zero);
        NativeMethods.mrsPeerConnectionRegisterTrackAddedCallback(h, _trackAddedCb, IntPtr.Zero);
        NativeMethods.mrsPeerConnectionRegisterTrackRemovedCallback(h, _trackRemovedCb, IntPtr.Zero);

        _logger.LogInformation("PeerConnection initialized ({Handle})", h);
    }

    private TaskCompletionSource<string> BeginSdp()
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_sdpLock)
        {
            if (_pendingSdpTcs != null)
                throw new InvalidOperationException("An SDP operation is already in progress.");
            _pendingSdpTcs = tcs;
        }
        return tcs;
    }

    /// <summary>Creates a local SDP offer and returns its text once the native side signals it.</summary>
    public Task<string> CreateOfferAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        var tcs = BeginSdp();
        using var reg = ct.Register(() => tcs.TrySetCanceled());
        int rc = NativeMethods.mrsPeerConnectionCreateOffer(_handle.DangerousGetHandle());
        if (rc != (int)MrsResult.Success)
        {
            lock (_sdpLock) _pendingSdpTcs = null;
            throw new WebRtcSdpException($"mrsPeerConnectionCreateOffer returned {rc}.");
        }
        _logger.LogDebug("CreateOfferAsync dispatched");
        return tcs.Task;
    }

    /// <summary>Creates a local SDP answer and returns its text once the native side signals it.</summary>
    public Task<string> CreateAnswerAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        var tcs = BeginSdp();
        using var reg = ct.Register(() => tcs.TrySetCanceled());
        int rc = NativeMethods.mrsPeerConnectionCreateAnswer(_handle.DangerousGetHandle());
        if (rc != (int)MrsResult.Success)
        {
            lock (_sdpLock) _pendingSdpTcs = null;
            throw new WebRtcSdpException($"mrsPeerConnectionCreateAnswer returned {rc}.");
        }
        _logger.LogDebug("CreateAnswerAsync dispatched");
        return tcs.Task;
    }

    /// <summary>Sets the local description. The SDP text is produced by Create*Async; calling this is a no-op relative to the native API and exists for API symmetry.</summary>
    public Task SetLocalDescriptionAsync(string type, string sdp, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(sdp);
        ct.ThrowIfCancellationRequested();
        _logger.LogDebug("SetLocalDescriptionAsync type={Type} length={Length}", type, sdp.Length);
        return Task.CompletedTask;
    }

    /// <summary>Sets the remote description received from the signaling channel.</summary>
    public Task SetRemoteDescriptionAsync(string type, string sdp, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(sdp);
        ct.ThrowIfCancellationRequested();
        int rc = NativeMethods.mrsPeerConnectionSetRemoteDescription(_handle.DangerousGetHandle(), type, sdp);
        if (rc != (int)MrsResult.Success)
            throw new WebRtcSdpException($"mrsPeerConnectionSetRemoteDescription returned {rc}.");
        _logger.LogDebug("SetRemoteDescription applied type={Type}", type);
        return Task.CompletedTask;
    }

    /// <summary>Feeds a remote ICE candidate received from the signaling channel.</summary>
    public void AddIceCandidate(string sdpMid, int sdpMlineIndex, string candidate)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(sdpMid);
        ArgumentNullException.ThrowIfNull(candidate);
        int rc = NativeMethods.mrsPeerConnectionAddIceCandidate(_handle.DangerousGetHandle(), sdpMid, sdpMlineIndex, candidate);
        if (rc != (int)MrsResult.Success)
            throw new WebRtcIceException($"mrsPeerConnectionAddIceCandidate returned {rc}.");
        _logger.LogDebug("AddIceCandidate mid={Mid} idx={Idx}", sdpMid, sdpMlineIndex);
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
            throw new ObjectDisposedException(nameof(PeerConnection));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _logger.LogInformation("PeerConnection disposing");

        lock (_sdpLock)
        {
            _pendingSdpTcs?.TrySetCanceled();
            _pendingSdpTcs = null;
        }

        _mediaLine.Dispose();

        try { _handle.Dispose(); } catch { }

        if (_connectedHandle.IsAllocated) _connectedHandle.Free();
        if (_disconnectedHandle.IsAllocated) _disconnectedHandle.Free();
        if (_iceHandle.IsAllocated) _iceHandle.Free();
        if (_sdpHandle.IsAllocated) _sdpHandle.Free();
        if (_trackAddedHandle.IsAllocated) _trackAddedHandle.Free();
        if (_trackRemovedHandle.IsAllocated) _trackRemovedHandle.Free();

        _workQueue.Dispose();
    }
}

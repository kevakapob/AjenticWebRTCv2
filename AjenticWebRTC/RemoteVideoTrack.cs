using System;
using System.Runtime.InteropServices;
using System.Threading;
using AjenticWebRTC.Interop;
using AjenticWebRTC.Logging;
using Microsoft.Extensions.Logging;

namespace AjenticWebRTC;

/// <summary>A remote video track that delivers I420A frames to managed code.</summary>
public sealed class RemoteVideoTrack : IDisposable
{
    private readonly IntPtr _trackHandle;
    private readonly ILogger _logger;
    private readonly WorkQueue _workQueue;
    private readonly I420AFrameReadyCallback _callback;
    private GCHandle _callbackHandle;
    private int _disposed;

    /// <summary>Raised on the work queue thread when a new video frame is available.</summary>
    public event EventHandler<VideoFrame>? VideoFrameReady;

    /// <summary>Native handle of the underlying track.</summary>
    public IntPtr NativeHandle => _trackHandle;

    /// <summary>Creates a wrapper around a native remote video track handle.</summary>
    public RemoteVideoTrack(IntPtr trackHandle, ILogger? logger, WorkQueue workQueue)
    {
        if (trackHandle == IntPtr.Zero) throw new ArgumentException("Track handle cannot be zero.", nameof(trackHandle));
        ArgumentNullException.ThrowIfNull(workQueue);
        _trackHandle = trackHandle;
        _logger = logger ?? NullWebRtcLogger.Instance;
        _workQueue = workQueue;
        // I420AFrameReadyCallback is an unsafe delegate type, so the assignment
        // must occur inside an unsafe context even though OnI420AFrame is itself unsafe.
        unsafe { _callback = OnI420AFrame; }
        _callbackHandle = GCHandle.Alloc(_callback);
        NativeMethods.mrsRemoteVideoTrackRegisterI420AFrameCallback(_trackHandle, _callback, IntPtr.Zero);
        _logger.LogDebug("RemoteVideoTrack registered I420A callback for handle {Handle}", _trackHandle);
    }

    private unsafe void OnI420AFrame(
        IntPtr userData,
        byte* yData, byte* uData, byte* vData, byte* aData,
        int strideY, int strideU, int strideV, int strideA,
        int width, int height)
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        if (width <= 0 || height <= 0) return;

        int ySize = strideY * height;
        int chromaHeight = (height + 1) / 2;
        int uSize = strideU * chromaHeight;
        int vSize = strideV * chromaHeight;

        var y = new byte[ySize];
        var u = new byte[uSize];
        var v = new byte[vSize];
        byte[]? a = null;

        fixed (byte* yDst = y) Buffer.MemoryCopy(yData, yDst, ySize, ySize);
        fixed (byte* uDst = u) Buffer.MemoryCopy(uData, uDst, uSize, uSize);
        fixed (byte* vDst = v) Buffer.MemoryCopy(vData, vDst, vSize, vSize);

        if (aData != null && strideA > 0)
        {
            int aSize = strideA * height;
            a = new byte[aSize];
            fixed (byte* aDst = a) Buffer.MemoryCopy(aData, aDst, aSize, aSize);
        }

        var frame = new VideoFrame
        {
            Width = width,
            Height = height,
            YPlane = y,
            UPlane = u,
            VPlane = v,
            APlane = a,
            StrideY = strideY,
            StrideU = strideU,
            StrideV = strideV,
            StrideA = strideA,
        };

        try
        {
            _workQueue.Enqueue(() => VideoFrameReady?.Invoke(this, frame));
        }
        catch (ObjectDisposedException) { }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        try
        {
            NativeMethods.mrsRemoteVideoTrackUnregisterI420AFrameCallback(_trackHandle);
        }
        catch (DllNotFoundException) { }
        catch (EntryPointNotFoundException) { }
        if (_callbackHandle.IsAllocated) _callbackHandle.Free();
        _logger.LogDebug("RemoteVideoTrack disposed for handle {Handle}", _trackHandle);
    }
}

using System;
using System.Runtime.InteropServices;

namespace AjenticWebRTC.Interop;

/// <summary>Kind of media track.</summary>
public enum TrackKind
{
    /// <summary>Audio track.</summary>
    Audio = 0,
    /// <summary>Video track.</summary>
    Video = 1,
}

/// <summary>Result codes returned by native mrwebrtc functions.</summary>
public enum MrsResult
{
    /// <summary>Operation succeeded.</summary>
    Success = 0,
}

/// <summary>Configuration passed to native <c>mrsPeerConnectionCreate</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct MrsPeerConnectionConfiguration
{
    /// <summary>Semicolon-separated list of ICE servers (e.g. <c>stun:stun.l.google.com:19302</c>).</summary>
    [MarshalAs(UnmanagedType.LPStr)]
    public string? IceServers;

    /// <summary>ICE transport type (0 = all, 1 = relay).</summary>
    public int IceTransportType;

    /// <summary>Bundle policy (0 = balanced, 1 = max-bundle, 2 = max-compat).</summary>
    public int BundlePolicy;

    /// <summary>SDP semantic (0 = unified-plan, 1 = plan-b).</summary>
    public int SdpSemantic;
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void PeerConnectionConnectedCallback(IntPtr userData);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void PeerConnectionDisconnectedCallback(IntPtr userData);

[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public delegate void PeerConnectionIceCandidateReadyCallback(
    IntPtr userData,
    [MarshalAs(UnmanagedType.LPStr)] string candidate,
    int sdpMlineIndex,
    [MarshalAs(UnmanagedType.LPStr)] string sdpMid);

[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public delegate void PeerConnectionLocalSdpReadyCallback(
    IntPtr userData,
    [MarshalAs(UnmanagedType.LPStr)] string type,
    [MarshalAs(UnmanagedType.LPStr)] string sdp);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void PeerConnectionTrackAddedCallback(IntPtr userData, TrackKind trackKind, IntPtr trackHandle);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void PeerConnectionTrackRemovedCallback(IntPtr userData, TrackKind trackKind, IntPtr trackHandle);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void I420AFrameReadyCallback(
    IntPtr userData,
    byte* yData, byte* uData, byte* vData, byte* aData,
    int strideY, int strideU, int strideV, int strideA,
    int width, int height);

internal static class NativeMethods
{
    private const string DllName = "mrwebrtc";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int mrsPeerConnectionCreate(ref MrsPeerConnectionConfiguration config, out IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void mrsPeerConnectionRegisterConnectedCallback(IntPtr handle, PeerConnectionConnectedCallback cb, IntPtr userData);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void mrsPeerConnectionRegisterDisconnectedCallback(IntPtr handle, PeerConnectionDisconnectedCallback cb, IntPtr userData);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void mrsPeerConnectionRegisterIceCandidateReadytoSendCallback(IntPtr handle, PeerConnectionIceCandidateReadyCallback cb, IntPtr userData);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void mrsPeerConnectionRegisterLocalSdpReadytoSendCallback(IntPtr handle, PeerConnectionLocalSdpReadyCallback cb, IntPtr userData);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void mrsPeerConnectionRegisterTrackAddedCallback(IntPtr handle, PeerConnectionTrackAddedCallback cb, IntPtr userData);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void mrsPeerConnectionRegisterTrackRemovedCallback(IntPtr handle, PeerConnectionTrackRemovedCallback cb, IntPtr userData);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int mrsPeerConnectionCreateOffer(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int mrsPeerConnectionCreateAnswer(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern int mrsPeerConnectionSetRemoteDescription(
        IntPtr handle,
        [MarshalAs(UnmanagedType.LPStr)] string kind,
        [MarshalAs(UnmanagedType.LPStr)] string sdp);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern int mrsPeerConnectionAddIceCandidate(
        IntPtr handle,
        [MarshalAs(UnmanagedType.LPStr)] string sdpMid,
        int sdpMlineIndex,
        [MarshalAs(UnmanagedType.LPStr)] string candidate);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void mrsPeerConnectionClose(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void mrsRemoteVideoTrackRegisterI420AFrameCallback(IntPtr trackHandle, I420AFrameReadyCallback cb, IntPtr userData);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void mrsRemoteVideoTrackUnregisterI420AFrameCallback(IntPtr trackHandle);
}

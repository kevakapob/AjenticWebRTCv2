using System;
using Microsoft.Win32.SafeHandles;

namespace AjenticWebRTC.Interop;

/// <summary>Safe handle wrapping a native mrwebrtc peer connection pointer.</summary>
public sealed class PeerConnectionHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    /// <summary>Initializes a new empty, invalid handle.</summary>
    public PeerConnectionHandle() : base(ownsHandle: true) { }

    /// <summary>Initializes a handle that wraps an existing native pointer.</summary>
    public PeerConnectionHandle(IntPtr existingHandle, bool ownsHandle) : base(ownsHandle)
    {
        SetHandle(existingHandle);
    }

    /// <summary>Sets the underlying native pointer after allocation.</summary>
    internal void SetNativeHandle(IntPtr value) => SetHandle(value);

    /// <inheritdoc/>
    protected override bool ReleaseHandle()
    {
        if (handle != IntPtr.Zero)
        {
            NativeMethods.mrsPeerConnectionClose(handle);
            SetHandle(IntPtr.Zero);
        }
        return true;
    }
}

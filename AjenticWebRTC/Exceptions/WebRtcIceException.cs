using System;

namespace AjenticWebRTC.Exceptions;

/// <summary>Thrown when an ICE candidate operation fails.</summary>
public sealed class WebRtcIceException : WebRtcException
{
    /// <inheritdoc/>
    public WebRtcIceException() { }
    /// <inheritdoc/>
    public WebRtcIceException(string message) : base(message) { }
    /// <inheritdoc/>
    public WebRtcIceException(string message, Exception? innerException) : base(message, innerException) { }
}

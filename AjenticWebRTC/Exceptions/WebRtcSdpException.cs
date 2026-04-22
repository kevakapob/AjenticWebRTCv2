using System;

namespace AjenticWebRTC.Exceptions;

/// <summary>Thrown when an SDP operation (offer/answer/set-description) fails.</summary>
public sealed class WebRtcSdpException : WebRtcException
{
    /// <inheritdoc/>
    public WebRtcSdpException() { }
    /// <inheritdoc/>
    public WebRtcSdpException(string message) : base(message) { }
    /// <inheritdoc/>
    public WebRtcSdpException(string message, Exception? innerException) : base(message, innerException) { }
}

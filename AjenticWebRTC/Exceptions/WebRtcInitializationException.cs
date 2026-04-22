using System;

namespace AjenticWebRTC.Exceptions;

/// <summary>Thrown when the native peer connection fails to initialize.</summary>
public sealed class WebRtcInitializationException : WebRtcException
{
    /// <inheritdoc/>
    public WebRtcInitializationException() { }
    /// <inheritdoc/>
    public WebRtcInitializationException(string message) : base(message) { }
    /// <inheritdoc/>
    public WebRtcInitializationException(string message, Exception? innerException) : base(message, innerException) { }
}

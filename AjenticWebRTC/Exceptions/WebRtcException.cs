using System;

namespace AjenticWebRTC.Exceptions;

/// <summary>Base exception for all WebRTC binding errors.</summary>
public class WebRtcException : Exception
{
    /// <inheritdoc/>
    public WebRtcException() { }
    /// <inheritdoc/>
    public WebRtcException(string message) : base(message) { }
    /// <inheritdoc/>
    public WebRtcException(string message, Exception? innerException) : base(message, innerException) { }
}

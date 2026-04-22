namespace AjenticWebRTC;

/// <summary>Immutable I420/I420A video frame delivered to managed code.</summary>
public readonly struct VideoFrame
{
    /// <summary>Width in pixels.</summary>
    public int Width { get; init; }
    /// <summary>Height in pixels.</summary>
    public int Height { get; init; }
    /// <summary>Y (luma) plane.</summary>
    public byte[] YPlane { get; init; }
    /// <summary>U (chroma) plane.</summary>
    public byte[] UPlane { get; init; }
    /// <summary>V (chroma) plane.</summary>
    public byte[] VPlane { get; init; }
    /// <summary>Optional alpha plane. Null when the frame has no alpha channel.</summary>
    public byte[]? APlane { get; init; }
    /// <summary>Row stride of <see cref="YPlane"/>.</summary>
    public int StrideY { get; init; }
    /// <summary>Row stride of <see cref="UPlane"/>.</summary>
    public int StrideU { get; init; }
    /// <summary>Row stride of <see cref="VPlane"/>.</summary>
    public int StrideV { get; init; }
    /// <summary>Row stride of <see cref="APlane"/>, or 0 when no alpha.</summary>
    public int StrideA { get; init; }
}

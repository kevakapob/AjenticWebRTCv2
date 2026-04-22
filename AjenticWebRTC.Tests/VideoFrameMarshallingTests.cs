using AjenticWebRTC;
using Xunit;

namespace AjenticWebRTC.Tests;

public class VideoFrameMarshallingTests
{
    [Fact]
    public void VideoFrame_HoldsPlaneData()
    {
        var frame = new VideoFrame
        {
            Width = 640,
            Height = 480,
            YPlane = new byte[] { 1, 2, 3 },
            UPlane = new byte[] { 4, 5 },
            VPlane = new byte[] { 6, 7 },
            StrideY = 640,
            StrideU = 320,
            StrideV = 320,
            StrideA = 0,
        };

        Assert.Equal(640, frame.Width);
        Assert.Equal(480, frame.Height);
        Assert.Equal(new byte[] { 1, 2, 3 }, frame.YPlane);
        Assert.Equal(new byte[] { 4, 5 }, frame.UPlane);
        Assert.Equal(new byte[] { 6, 7 }, frame.VPlane);
        Assert.Null(frame.APlane);
    }

    [Fact]
    public void VideoFrame_StridesPreserved()
    {
        var frame = new VideoFrame
        {
            Width = 2, Height = 2,
            YPlane = new byte[] { 0, 0, 0, 0 },
            UPlane = new byte[] { 0 },
            VPlane = new byte[] { 0 },
            StrideY = 2, StrideU = 1, StrideV = 1, StrideA = 2,
        };

        Assert.Equal(2, frame.StrideY);
        Assert.Equal(1, frame.StrideU);
        Assert.Equal(1, frame.StrideV);
        Assert.Equal(2, frame.StrideA);
    }

    [Fact]
    public void VideoFrame_APlane_Nullable()
    {
        var withAlpha = new VideoFrame
        {
            Width = 1, Height = 1,
            YPlane = new byte[] { 1 },
            UPlane = new byte[] { 1 },
            VPlane = new byte[] { 1 },
            APlane = new byte[] { 0xFF },
            StrideY = 1, StrideU = 1, StrideV = 1, StrideA = 1,
        };
        Assert.NotNull(withAlpha.APlane);
        Assert.Equal(0xFF, withAlpha.APlane![0]);

        var withoutAlpha = new VideoFrame
        {
            Width = 1, Height = 1,
            YPlane = new byte[] { 1 },
            UPlane = new byte[] { 1 },
            VPlane = new byte[] { 1 },
            StrideY = 1, StrideU = 1, StrideV = 1, StrideA = 0,
        };
        Assert.Null(withoutAlpha.APlane);
    }
}

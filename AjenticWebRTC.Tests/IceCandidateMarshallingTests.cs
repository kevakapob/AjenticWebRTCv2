using AjenticWebRTC;
using Xunit;

namespace AjenticWebRTC.Tests;

public class IceCandidateMarshallingTests
{
    [Fact]
    public void IceCandidateReadyEventArgs_StoresValues()
    {
        var args = new IceCandidateReadyEventArgs(
            candidate: "candidate:1 1 udp 2122260223 192.168.1.2 54321 typ host",
            sdpMlineIndex: 0,
            sdpMid: "video");

        Assert.StartsWith("candidate:", args.Candidate);
        Assert.Equal(0, args.SdpMlineIndex);
        Assert.Equal("video", args.SdpMid);
    }

    [Fact]
    public void IceCandidateReadyEventArgs_AcceptsNonZeroMlineIndex()
    {
        var args = new IceCandidateReadyEventArgs("c", 3, "audio");
        Assert.Equal(3, args.SdpMlineIndex);
        Assert.Equal("audio", args.SdpMid);
        Assert.Equal("c", args.Candidate);
    }
}

using AjenticWebRTC;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AjenticWebRTC.Tests;

public class PeerConnectionTests
{
    [Fact]
    public void PeerConnectionConfiguration_DefaultValues()
    {
        var cfg = new PeerConnectionConfiguration();
        Assert.Equal(string.Empty, cfg.IceServers);
        Assert.Equal(0, cfg.IceTransportType);
        Assert.Equal(0, cfg.BundlePolicy);
        Assert.Equal(0, cfg.SdpSemantic);
    }

    [Fact]
    public void PeerConnectionConfiguration_CanSetIceServers()
    {
        var cfg = new PeerConnectionConfiguration { IceServers = "stun:stun.l.google.com:19302" };
        Assert.Equal("stun:stun.l.google.com:19302", cfg.IceServers);
    }

    [Fact]
    public void IceCandidateReadyEventArgs_Properties()
    {
        var a = new IceCandidateReadyEventArgs("cand", 1, "mid");
        Assert.Equal("cand", a.Candidate);
        Assert.Equal(1, a.SdpMlineIndex);
        Assert.Equal("mid", a.SdpMid);
    }

    [Fact]
    public void LocalSdpReadyEventArgs_Properties()
    {
        var a = new LocalSdpReadyEventArgs("offer", "v=0\r\n...");
        Assert.Equal("offer", a.Type);
        Assert.StartsWith("v=0", a.Sdp);
    }

    [Fact]
    public void MockLogger_CanBeConstructed()
    {
        var logger = new Mock<ILogger<PeerConnection>>();
        Assert.NotNull(logger.Object);
    }

    // Requires mrwebrtc.dll to be present next to the test assembly, which is not shipped in this repo.
    [Fact(Skip = "Requires mrwebrtc.dll")]
    public void PeerConnection_CanBeConstructed_WhenNativeAvailable()
    {
        using var pc = new PeerConnection(new PeerConnectionConfiguration
        {
            IceServers = "stun:stun.l.google.com:19302",
        });
        Assert.NotNull(pc.MediaLine);
    }
}

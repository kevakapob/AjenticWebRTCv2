// Sample console application demonstrating the AjenticWebRTC managed binding.
// NOTE: mrwebrtc.dll (from MixedReality-WebRTC v2) must be present alongside the
// executable at runtime, otherwise native calls will throw DllNotFoundException.

using System;
using System.Threading;
using System.Threading.Tasks;
using AjenticWebRTC;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(b => b
    .SetMinimumLevel(LogLevel.Debug)
    .AddConsole());
var logger = loggerFactory.CreateLogger<PeerConnection>();

var config = new PeerConnectionConfiguration
{
    IceServers = "stun:stun.l.google.com:19302",
};

try
{
    using var pc = new PeerConnection(config, logger);

    pc.Connected += (_, _) => Console.WriteLine("[event] Connected");
    pc.Disconnected += (_, _) => Console.WriteLine("[event] Disconnected");
    pc.IceCandidateReady += (_, e) =>
        Console.WriteLine($"[event] ICE candidate mid={e.SdpMid} idx={e.SdpMlineIndex}: {e.Candidate}");
    pc.LocalSdpReady += (_, e) =>
        Console.WriteLine($"[event] Local SDP {e.Type} ({e.Sdp.Length} chars)");

    pc.MediaLine.VideoTrackAdded += (_, track) =>
    {
        Console.WriteLine("[event] Video track added");
        track.VideoFrameReady += (_, frame) =>
            Console.WriteLine($"[video] {frame.Width}x{frame.Height} Y={frame.YPlane.Length}B");
    };

    Console.WriteLine("Creating offer...");
    var sdp = await pc.CreateOfferAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
    Console.WriteLine("Offer SDP:");
    Console.WriteLine(sdp);

    Console.WriteLine("Press ENTER to exit.");
    Console.ReadLine();
}
catch (DllNotFoundException)
{
    Console.Error.WriteLine("mrwebrtc.dll was not found. Place it next to the executable and retry.");
    return 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Fatal: {ex}");
    return 2;
}

return 0;

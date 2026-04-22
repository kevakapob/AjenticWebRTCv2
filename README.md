# AjenticWebRTC

A pure-managed .NET 8 binding to the MixedReality-WebRTC v2 native library (`mrwebrtc.dll`) — no Unity dependency.

## Requirements

- Windows x64
- .NET 8 SDK
- `mrwebrtc.dll` from the MixedReality-WebRTC v2 release, placed next to the consuming executable

## Solution structure

| Project | Description |
| --- | --- |
| `AjenticWebRTC` | The managed binding: P/Invoke layer, `PeerConnection`, `MediaLine`, `RemoteVideoTrack`, `WorkQueue`, and the I420A video pipeline. |
| `AjenticWebRTC.Tests` | xUnit unit tests covering the work queue, video frame marshalling, ICE candidate event args, and configuration defaults. Tests needing the native DLL are skipped. |
| `AjenticWebRTC.Sample` | Console sample that creates a peer connection, subscribes to events, and creates an SDP offer. |

## Build

```bash
dotnet build AjenticWebRTC.sln -c Release
```

## Tests

```bash
dotnet test AjenticWebRTC.sln
```

Tests marked `Skip = "Requires mrwebrtc.dll"` will be skipped unless the native library is available at runtime.

## Usage

```csharp
using AjenticWebRTC;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
var logger = loggerFactory.CreateLogger<PeerConnection>();

using var pc = new PeerConnection(new PeerConnectionConfiguration
{
    IceServers = "stun:stun.l.google.com:19302",
}, logger);

pc.LocalSdpReady += (_, e) => Console.WriteLine($"Local SDP {e.Type}\n{e.Sdp}");
pc.IceCandidateReady += (_, e) => Console.WriteLine($"ICE {e.SdpMid}#{e.SdpMlineIndex}: {e.Candidate}");
pc.MediaLine.VideoTrackAdded += (_, track) =>
    track.VideoFrameReady += (_, frame) => Console.WriteLine($"frame {frame.Width}x{frame.Height}");

string offer = await pc.CreateOfferAsync();
await pc.SetRemoteDescriptionAsync("answer", remoteSdpFromSignaling);
pc.AddIceCandidate(sdpMid, sdpMlineIndex, candidate);
```

## Memory management

- All unmanaged callback delegates are pinned with `GCHandle` for the lifetime of the owner and released during `Dispose`.
- The native peer connection handle is wrapped in a `SafeHandleZeroOrMinusOneIsInvalid`-derived type that calls `mrsPeerConnectionClose` in its finalizer/release path.
- Video frames arrive on native threads; all Y/U/V/A planes are copied into managed `byte[]` arrays immediately, then dispatched to the `WorkQueue`. The application observes frames on the serial work-queue thread, never on a native thread.
- `RemoteVideoTrack.Dispose` unregisters the native frame callback before freeing its pinned delegate.
- `PeerConnection.Dispose` cancels any pending SDP task, disposes the media line, releases the safe handle, frees all pinned callback delegates, and stops the work queue.

## No Unity dependency

This binding is a plain .NET 8 library. It does not reference UnityEngine, the MRWebRTC Unity package, or any Unity tooling. It can be consumed from any .NET 8 Windows x64 application.

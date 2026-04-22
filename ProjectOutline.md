# Project Outline: Custom WebRTC v2 Managed Binding for .NET 8

## 1. Project Purpose
Create a fully custom, production-grade .NET 8 managed binding for the MixedReality-WebRTC v2 native library (mrwebrtc.dll), without relying on Unity or Unity-specific C# layers.

## 2. High-Level Objectives
Implement a clean, idiomatic .NET 8 API that exposes:
• PeerConnection lifecycle
• SDP offer/answer creation
• ICE candidate exchange
• Media track negotiation
• Remote video frame delivery (I420A)
• Optional audio track support
• Thread-safe callback dispatch

## 3. Constraints and Requirements
• Must not reference UnityEngine or Unity APIs.
• Must not depend on Unity’s threading, events, or GameObject lifecycle.
• Must interoperate directly with mrwebrtc.dll via P/Invoke.
• Must support Windows x86_64 only (matching available native binaries).
• Must target .NET 8 and allow unsafe code.
• Must provide deterministic memory handling for native buffers.
• Must expose async-friendly APIs where appropriate.

## 4. Native Interop Layer Requirements
Define P/Invoke signatures for:
• PeerConnection creation, initialization, and shutdown
• Setting local and remote descriptions
• Creating offers and answers
• Adding ICE candidates
• Registering native callbacks for:
  - OnConnected
  - OnDisconnected
  - OnIceCandidate
  - OnLocalSdpReady
  - OnTrackAdded
  - OnTrackRemoved
  - OnI420AFrameReady

## 5. Managed API Architecture
Implement the following classes:
• PeerConnection
  - Wraps native handle
  - Manages lifecycle
  - Exposes async SDP operations
  - Raises .NET events for native callbacks

• MediaLine
  - Represents negotiated audio/video tracks
  - Maps native track handles to managed objects

• RemoteVideoTrack
  - Receives I420A frames
  - Converts native frame buffers into managed structs

• WorkQueue
  - Provides thread marshalling for native callbacks
  - Ensures callbacks execute on a safe .NET thread

## 6. Memory Management Requirements
• All native pointers must be wrapped in SafeHandle or equivalent.
• All frame buffers must be copied before returning to user code.
• No managed object may hold a raw pointer beyond the callback scope.
• All native allocations must be paired with explicit frees where required.

## 7. Event and Callback Model
• Native callbacks must be registered during PeerConnection initialization.
• Callbacks must be forwarded to WorkQueue for safe execution.
• Managed events must follow .NET conventions:
  - EventHandler<T>
  - CancellationToken support where appropriate

## 8. SDP and ICE Handling
Implement:
• CreateOfferAsync()
• CreateAnswerAsync()
• SetLocalDescriptionAsync()
• SetRemoteDescriptionAsync()
• AddIceCandidate()

Native callback for local SDP must:
• Marshal SDP text into managed string
• Raise LocalSdpReady event

Native callback for ICE candidates must:
• Marshal candidate string and metadata
• Raise IceCandidateReady event

## 9. Video Frame Pipeline
Implement I420A frame delivery:
• Native callback receives:
  - width
  - height
  - strideY, strideU, strideV, strideA
  - pointers to Y, U, V, A planes

• Managed layer must:
  - Copy planes into managed byte arrays
  - Package into a VideoFrame struct
  - Raise VideoFrameReady event

## 10. Error Handling Strategy
• All native calls must check return codes.
• Throw managed exceptions for:
  - Initialization failures
  - Invalid SDP
  - Invalid ICE candidates
  - Native interop errors

## 11. Logging and Diagnostics
• Provide optional ILogger<T> integration.
• Log:
  - Native errors
  - Callback registration
  - SDP negotiation steps
  - Track creation/removal

## 12. Testing Requirements
Unit tests must cover:
• PeerConnection lifecycle
• SDP creation and parsing
• ICE candidate marshalling
• Video frame marshalling
• Thread marshalling via WorkQueue

Integration tests must:
• Use a loopback PeerConnection pair
• Validate offer/answer exchange
• Validate ICE connectivity
• Validate video frame delivery

## 13. Deliverables
• Complete .NET 8 class library project
• Fully documented public API
• P/Invoke interop layer
• Managed PeerConnection implementation
• Managed RemoteVideoTrack implementation
• WorkQueue thread marshaller
• Unit and integration tests
• Example usage code for:
  - Creating a PeerConnection
  - Exchanging SDP
  - Receiving video frames

## 14. Stretch Goals (Optional)
• Audio track support
• Hardware acceleration hooks
• Cross-platform support (Linux/macOS)
• WebRTC data channels

## 15. Completion Criteria
The project is complete when:
• A .NET 8 application can:
  - Create a PeerConnection
  - Exchange SDP with a browser
  - Establish ICE connectivity
  - Receive I420A video frames
• All tests pass
• No Unity dependencies remain
• No memory leaks occur under load

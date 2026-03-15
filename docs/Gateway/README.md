# Gateway Service - Comprehensive Documentation

## Overview
The **Gateway Service** is the real-time communication hub of the ChatSystem. It manages WebSocket connections, handles message routing, presence tracking, and WebRTC signaling for voice/video calls. It acts as the bridge between clients and the backend services via RabbitMQ.

## Technology Stack
- **Framework**: ASP.NET Core 8.0/9.0
- **Real-time Communication**: WebSockets
- **Messaging**: MassTransit with RabbitMQ
- **State Management**: In-memory presence & session stores
- **Serialization**: MessagePack (binary) & JSON

---

## Table of Contents
1. [Project Structure](#project-structure)
2. [Core Components](#core-components)
3. [Features](#features)
4. [Message Flow](#message-flow)
5. [WebRTC Signaling](#webrtc-signaling)
6. [Presence System](#presence-system)
7. [Broadcasting](#broadcasting)
8. [Configuration](#configuration)

---

## Project Structure

```
Gateway/
├── Gateway/                       # Main Gateway Project
│   ├── Middleware/                # WebSocket Middleware
│   │   └── WebSocketMiddleware.cs # Entry point for WS connections
│   ├── Program.cs                 # Entry point
│   └── appsettings.json          # Configuration
├── Application/                   # Application Layer
│   ├── Abstractions/              # Interfaces
│   │   ├── Handler/              # Method handlers
│   │   ├── Publisher/            # Message publisher interface
│   │   ├── Connection/           # Connection management
│   │   └── Broadcast/            # Broadcasting services
│   ├── Handlers/                  # Message handlers
│   │   ├── Message/              # Message-related handlers
│   │   ├── Call/                 # WebRTC call handlers
│   │   ├── State/                # State query handlers
│   │   ├── Sync/                 # Sync handlers
│   │   └── Heartbeat/            # Heartbeat handler
│   └── Dtos/                     # Data Transfer Objects
├── Domain/                        # Domain Layer
│   └── Models/                   # Domain models
└── Infrastructure/               # Infrastructure Layer
    ├── Handler/
    │   ├── WebSocketHandler/
    │   │   ├── Ingress/          # Incoming message handling
    │   │   └── Engress/          # Outgoing message handling (Consumers)
    ├── Repositories/             # Repository implementations
    └── Services/                  # Business services
        ├── Auth/                 # Authentication services
        ├── Session/              # Session management
        ├── Connection/          # Connection & presence
        ├── Broadcast/            # Message broadcasting
        ├── Queue/                # Queue services
        └── Publisher/            # RabbitMQ publisher
```

---

## Core Components

### 1. WebSocketMiddleware
The entry point for all WebSocket connections.

**Path**: `/ws`

**Key Responsibilities:**
- Accept WebSocket requests at `/ws` endpoint
- Validate JWT authentication
- Extract user ID from token claims
- Delegate connection handling to `IGatewayIngressHandler`

**Flow:**
```
Client → /ws endpoint → JWT Validation → Extract UserId → GatewayIngressHandler
```

**Code Reference:**
```csharp
if (context.Request.Path.StartsWithSegments("/ws"))
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }
    
    if (context.User?.Identity?.IsAuthenticated != true)
    {
        context.Response.StatusCode = 401;
        return;
    }
    
    var socket = await context.WebSockets.AcceptWebSocketAsync();
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    var gateway = scope.ServiceProvider.GetRequiredService<IGatewayIngressHandler>();
    await gateway.HandleAsync(userId, socket, context.RequestAborted);
}
```

---

### 2. GatewayIngressHandler & Message Pipeline
Handles incoming WebSocket messages (ingress) with high efficiency.

**Key Features:**
- **Pipelines Integration**: Uses `FrameReader` (built on `System.IO.Pipelines`) to read frames without unnecessary allocations.
- **Custom Binary Framing**:
  - `Header`: 5 bytes (4 bytes Length + 1 byte Type)
  - `Payload`: Binary data (MessagePack serialized)
- **Method Dispatcher**: Uses `IMethodDispatcher` for O(1) routing to handlers.

**Pipeline Flow:**
1. `GatewayIngressHandler` accepts connection.
2. `FrameReader` starts reading from the `Pipe`.
3. `ProcessFrameAsync` identifies frame type (Message, Ping, Pong, Close).
4. `ProcessMessageFrameAsync` deserializes the `MessageEnvelope` using `MessageSerializer` (MessagePack).
5. `MethodDispatcher` resolves the handler and executes it.

**Supported Methods:**
| Method | Handler | Description |
|--------|---------|-------------|
| `NewMessage` | NewMessageMethodHandler | Send new message |
| `ReceivedACK` | MessageReceivedAckMethodHandler | Message delivered acknowledgment |
| `SeenACK` | MessageSeenAckMethodHandler | Message read acknowledgment |
| `offer` | OfferMethodHandler | WebRTC offer (call initiation) |
| `answer` | AnswerMethodHandler | WebRTC answer |
| `ice_candidate` | IceCandidateMethodHandler | ICE candidate exchange |
| `join_call` | JoinCallMethodHandler | Join ongoing call |
| `leave_call` | LeaveCallHandler | Leave call |
| `create_call` | CreateGroupCallHandler | Create group call |
| `media_state` | MediaStateHandler | Update media state (mute/camera) |
| `get_user_state` | UserStateMethodHndler | Get user online status |
| `get_group_state` | GroupStateMethodHndler | Get group presence |
| `sync_user` | SyncUserAckMethodHanlder | Sync user data |

---

### 3. BroadcastManager & Egress Pipeline
Manages high-throughput message distribution.

**Key Features:**
- **Zero-Copy Serialization**: Leverages `ReadOnlyMemory<byte>` for efficient data handling.
- **Parallel Fan-out**: Uses `Parallel.ForEachAsync` with a `MaxDegreeOfParallelism` (default: 100) to ensure that slow clients don't block the entire broadcast queue.
- **Resilience**: Individually catches socket exceptions to prevent a single failing connection from aborting a multi-user broadcast.

---

## Features

### 1. Real-Time Messaging

#### Sending a Message
```
Client → WebSocket: { "method": "NewMessage", "params": { ... } }
Gateway → RabbitMQ: InsertMessageCommand
Worker → MongoDB: Save message
Worker → RabbitMQ: MessageCreatedEvent
BroadcastPrepWorker → RabbitMQ: BroadcastMessageCommand
Gateway ← RabbitMQ: BroadcastMessageCommand
Gateway → Client: Push message via WebSocket
```

**Handler Code:**
```csharp
public class NewMessageMethodHandler : BaseMethodHandler<InsertMessageCommand>
{
    public override string MethodName => "NewMessage";

    protected override async Task HandleAsync(string userId, InsertMessageCommand request, WebSocket socket)
    {
        await _publisher.PublishAsync(request);
    }
}
```

---

### 2. Message Acknowledgment (ACK)

#### Delivery ACK
When a client receives a message, it sends a delivery acknowledgment:

```
Client → WebSocket: { 
  "method": "ReceivedACK", 
  "params": { 
    "chatId": "xxx", 
    "messageId": "yyy", 
    "receivedAt": "2024-01-01T00:00:00Z" 
  } 
}
```

**Handler Code:**
```csharp
public class MessageReceivedAckMethodHandler : BaseMethodHandler<MessageReceivedAckEvent>
{
    public override string MethodName => "ReceivedACK";

    protected override async Task HandleAsync(string userId, MessageReceivedAckEvent request, WebSocket socket)
    {
        await _publisher.PublishAsync(new MessageDeliveredCommand
        {
            ChatId = request.ChatId,
            MessageId = request.MessageId,
            ReceiverId = userId,
            DeliveredAt = request.ReceivedAt
        });
    }
}
```

#### Seen ACK
When a user views messages:
```
Client → WebSocket: { "method": "SeenACK", "params": { "chatId": "xxx", "messageIds": [...] } }
```

---

## WebRTC Signaling

The Gateway handles WebRTC signaling for P2P and group video/voice calls.

### Call Flow

```
┌─────────┐         ┌──────────┐         ┌─────────┐
│ Caller  │         │ Gateway  │         │ Callee │
└────┬────┘         └────┬─────┘         └───┬────┘
     │                  │                    │
     │ ── offer ──────► │                    │
     │                  │ ── offer ────────► │
     │                  │                    │
     │                  │ ◄── answer ────────│
     │ ◄─ answer ────── │                    │
     │                  │                    │
     │ ── ice_candidate ►                   │
     │                  │ ── ice_candidate ► │
     │                  │ ◄─ ice_candidate ─│
     │ ◄─ ice_candidate │                    │
     │                  │                    │
     │    P2P Connection Established       │
```

### Call Handlers

#### 1. OfferMethodHandler
Handles call initiation (offer).

```csharp
public class OfferMethodHandler : BaseMethodHandler<OfferSignal>
{
    public override string MethodName => "offer";

    protected override async Task HandleAsync(string userId, OfferSignal request, WebSocket socket)
    {
        // Generate unique session ID
        var sessionId = Guid.NewGuid().ToString();
        
        // Store session in memory
        await _sessionStore.SetAsync(sessionId, new SessionCallInfo
        {
            SessionId = sessionId,
            Type = SessionType.Direct,
            CreatorId = userId,
            Participants = new List<string> { userId }
        });
        
        // Notify callee
        await _broadcastServices.SendMessageToUserAsync(request.TargetUserId, new
        {
            Method = "offer",
            Params = new { SessionId = sessionId, SenderId = userId, Sdp = request.Sdp }
        });
    }
}
```

#### 2. AnswerMethodHandler
Handles call acceptance (answer).

```csharp
public class AnswerMethodHandler : BaseMethodHandler<AnswerSignal>
{
    public override string MethodName => "answer";

    protected override async Task HandleAsync(string userId, AnswerSignal request, WebSocket socket)
    {
        // Get session and notify caller
        var session = await _sessionStore.GetAsync(request.SessionId);
        
        await _broadcastServices.SendMessageToUserAsync(session.CreatorId, new
        {
            Method = "answer",
            Params = new { SessionId = session.SessionId, Sdp = request.Sdp }
        });
    }
}
```

#### 3. IceCandidateMethodHandler
Handles ICE candidate exchange for network path discovery.

```csharp
public class IceCandidateMethodHandler : BaseMethodHandler<IceCandidateSignal>
{
    public override string MethodName => "ice_candidate";

    protected override async Task HandleAsync(string userId, IceCandidateSignal request, WebSocket socket)
    {
        var session = await _sessionStore.GetAsync(request.SessionId);
        
        // Exchange ICE candidates between participants
        foreach (var participantId in session.Participants)
        {
            if (participantId != userId)
            {
                await _broadcastServices.SendMessageToUserAsync(participantId, new
                {
                    Method = "ice_candidate",
                    Params = new { SessionId = session.SessionId, Candidate = request.Candidate }
                });
            }
        }
    }
}
```

#### 4. JoinCallMethodHandler
Handles joining an existing call.

#### 5. LeaveCallHandler
Handles leaving a call.

#### 6. MediaStateHandler
Synchronizes mute/unmute and camera on/off states.

```csharp
public class MediaStateHandler : BaseMethodHandler<MediaStateSignal>
{
    public override string MethodName => "media_state";

    protected override async Task HandleAsync(string userId, MediaStateSignal request, WebSocket socket)
    {
        // Broadcast media state to all participants
        var session = await _sessionStore.GetAsync(request.SessionId);
        
        foreach (var participantId in session.Participants)
        {
            await _broadcastServices.SendMessageToUserAsync(participantId, new
            {
                Method = "media_state",
                Params = new
                {
                    UserId = userId,
                    IsMuted = request.IsMuted,
                    IsCameraOn = request.IsCameraOn
                }
            });
        }
    }
}
```

### Call Session Management

**Ring Timeout:**
- Calls auto-cancel if not answered within 30 seconds
- Managed by `RingTimeoutService`

**Active Call Guard:**
- Prevents creating multiple calls for the same chat

---

## Presence System

The Gateway tracks user online/offline status in real-time.

### PresenceService

```csharp
public sealed class PresenceService : IPresenceService
{
    public async Task<UserPresence> GetPresenceAsync(string userId)
    {
        var activeSockets = _connectionStore.GetUserSockets(userId);
        
        if (activeSockets.Any())
            return UserPresence.Online(userId, activeSockets.Count);
        
        var lastSeen = await _presenceRepository.GetLastSeenAsync(userId);
        
        return lastSeen.HasValue
            ? UserPresence.Offline(userId, lastSeen.Value)
            : UserPresence.NeverConnected(userId);
    }
    
    public async Task OnDisconnectedAsync(string userId)
    {
        var activeSockets = _connectionStore.GetUserSockets(userId);
        
        if (!activeSockets.Any())
        {
            // No more active sockets - update last seen
            await _presenceRepository.SetLastSeenAsync(userId, DateTime.UtcNow);
        }
    }
}
```

### Presence States
| State | Description |
|-------|-------------|
| Online | User has active WebSocket connection(s) |
| Offline | User disconnected, last seen timestamp available |
| NeverConnected | User has never connected to the system |

### Querying Presence
```
Client → WebSocket: { "method": "get_user_state", "params": { "userId": "xxx" } }
Gateway → Client: { "status": "online|offline", "lastSeen": "..." }
```

---

## Broadcasting

### BroadcastServices
Handles message delivery to connected clients.

```csharp
public class BroadcastServices : IBroadcastServices
{
    public async Task SendMessageToUserAsync(string userId, object message)
    {
        // Get user's all sockets
        var sockets = fanOutResolver.Resolve(userId);
        
        if (!sockets.Any()) return;
        
        // Broadcast to all sockets
        await broadcastManager.BroadcastAsync(sockets, message.ToByteArray(), WebSocketMessageType.Binary);
    }
    
    public async Task SendMessageToGroupAsync(string senderId, string groupId, object message)
    {
        var sockets = fanOutResolver.Resolve(groupId, senderId);
        
        if (!sockets.Any()) return;
        
        await broadcastManager.BroadcastAsync(sockets, message.ToByteArray(), WebSocketMessageType.Binary);
    }
}
```

---

## Message Flow Diagrams

### Incoming Message Flow (Ingress)
```
WebSocket Message
       │
       ▼
WebSocketMiddleware
       │
       ▼
GatewayIngressHandler
       │
       ▼
Method Router (by method name)
       │
       ▼
Specific Handler (NewMessage, Offer, etc.)
       │
       ▼
RabbitMQ Publisher
       │
       ▼
Worker Service
```

### Outgoing Message Flow (Egress)
```
RabbitMQ Message
       │
       ▼
Consumer (BroadcastMessageConsumer, etc.)
       │
       ▼
BroadcastServices
       │
       ▼
FanOutResolver (find target sockets)
       │
       ▼
BroadcastManager
       │
       ▼
WebSocket Connections
```

---

## Configuration

### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "ConnectionStrings": {
    "MongoDB": "mongodb://localhost:27017/ChatSystem"
  },
  "JWT": {
    "SecretKey": "your-secret-key-here",
    "Issuer": "ChatSystem",
    "Audience": "ChatSystemClient"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest",
    "Port": 5672
  }
}
```

---

## Dependencies

### NuGet Packages
- `MassTransit` - Message broker abstraction
- `RabbitMQ.Client` - RabbitMQ client
- `Microsoft.AspNetCore.Authentication.JwtBearer` - JWT authentication
- `MessagePack-CSharp` - Binary serialization

---

## Background Services

### 1. MessageReceivedAckBackground
Processes batched message delivery acknowledgments.

### 2. BroadcastMessageBackground
Handles message broadcasting to connected clients.

### 3. CleanupConnactionBackground
Cleans up stale connections periodically.

---

## Future Improvements

1. **Redis Session Store**: Replace in-memory session store with Redis for multi-instance deployment
2. **Connection Rate Limiting**: Prevent connection abuse
3. **Message Queue Backpressure**: Handle high-load scenarios
4. **Metrics & Monitoring**: Add OpenTelemetry for observability
5. **WebSocket Compression**: Enable permessage-deflate compression


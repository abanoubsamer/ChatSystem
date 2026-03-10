# Worker Service - Comprehensive Documentation

## Overview
The **Worker Service** is the core business logic layer of the ChatSystem. It handles message persistence, manages chat state using Microsoft Orleans (Actor Model), processes acknowledgments, and manages call sessions. It serves as the "brain" of the system, coordinating between the Gateway and the database.

## Technology Stack
- **Framework**: ASP.NET Core 8.0/9.0
- **Actor Model**: Microsoft Orleans
- **Database**: MongoDB
- **Messaging**: MassTransit with RabbitMQ
- **Serialization**: BSON (MongoDB)

---

## Table of Contents
1. [Project Structure](#project-structure)
2. [Core Components](#core-components)
3. [Message Processing](#message-processing)
4. [Orleans State Management](#orleans-state-management)
5. [ACK Tracking System](#ack-tracking-system)
6. [Call Session Management](#call-session-management)
7. [Message Flow](#message-flow)
8. [Configuration](#configuration)

---

## Project Structure

```
Worker/
├── Worker/                         # Main Worker Project
│   ├── Program.cs                  # Entry point
│   ├── Worker.csproj
│   └── appsettings.json
├── Application/                    # Application Layer
│   ├── Abstractions/              # Interfaces
│   │   ├── Grain/                 # Orleans grain interfaces
│   │   ├── Repositories/          # Repository interfaces
│   │   └── Services/              # Service interfaces
│   ├── Dtos/                     # Data Transfer Objects
│   └── Result/                   # Result patterns
├── Domain/                         # Domain Layer
│   ├── Models/                    # Domain models
│   └── Enums/                     # Enumerations
└── Infrastructure/                # Infrastructure Layer
    ├── ChatGrain.cs               # Orleans grain implementation
    ├── ConsumerHandler/           # MassTransit consumers
    │   ├── Message/              # Message consumers
    │   │   ├── Commend/          # Command consumers
    │   │   └── Event/            # Event consumers
    │   ├── Call/                 # Call consumers
    │   ├── Chat/                 # Chat consumers
    │   └── Snapshot/            # Snapshot consumers
    ├── Repositories/              # Repository implementations
    └── Services/                  # Service implementations
        ├── Call/                  # Call management
        └── Chat/                  # Chat services
```

---

## Core Components

### 1. Orleans Grains

The Worker service uses **Microsoft Orleans** for distributed state management. Orleans is an implementation of the Actor Model that provides:

- **Distributed ACKs**: Track message delivery and read status across millions of users
- **In-memory state**: Ultra-fast state lookups without database queries
- **Automatic persistence**: Periodic state saves to MongoDB
- **Horizontal scaling**: Add more silos to handle more chats

#### ChatGrain
The main grain responsible for tracking message acknowledgments in a chat.

```csharp
public class ChatGrain : Grain, IChatGrain
{
    private readonly IPersistentState<ChatGrainState> _state;
    private readonly IPublishEndpoint _publisher;
    private IDisposable? _saveTimer;

    // Called when grain activates
    public override Task OnActivateAsync(CancellationToken ct)
    {
        // Auto-save to DB every 30 seconds
        _saveTimer = RegisterTimer(
            _ => _state.WriteStateAsync(),
            null,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30));
        
        return base.OnActivateAsync(ct);
    }
}
```

**ChatGrainState Structure:**
```csharp
public class ChatGrainState
{
    public Dictionary<string, int> MemberIndex { get; set; }  // memberId -> bitmap index
    public int NextIndex { get; set; }
    public int TotalMembers { get; set; }
    
    // Pending ACKs per message
    public Dictionary<string, int> PendingDelivery { get; set; }
    public Dictionary<string, int> PendingSeen { get; set; }
    
    // Bitmap tracking per message
    public Dictionary<string, byte[]> DeliveryBitmaps { get; set; }
    public Dictionary<string, byte[]> SeenBitmaps { get; set; }
    
    // Minimum message ID that has been fully acknowledged
    public string MinDelivery { get; set; }
    public string MinSeen { get; set; }
}
```

---

### 2. MassTransit Consumers

The Worker service consumes messages from RabbitMQ through MassTransit consumers.

#### InsertMessageConsumer
Handles new message creation.

```csharp
public class InsertMessageConsumer : IConsumer<InsertMessageCommand>
{
    private readonly IMessagesRepository _Messagerepo;
    private readonly IMessagePublisher _eventBus;
    private readonly IGrainFactory _grainFactory;

    public async Task Consume(ConsumeContext<InsertMessageCommand> context)
    {
        var command = context.Message;
        
        // Create message entity
        var newMessage = await CreateMessageAsync(command);
        
        // Persist to MongoDB
        await _Messagerepo.AddNewMessageAsync(newMessage);
        
        // Publish event for downstream processing
        await _eventBus.PublishAsync(new MessageCreatedEvent
        {
            MessageId = newMessage.Id.ToString(),
            ChatId = newMessage.ChatId,
            SenderId = newMessage.SenderId,
            Content = newMessage.Content,
            SentAt = newMessage.SentAt
        });
    }
}
```

#### UpdateDeliveryStatusConsumer
Processes delivery acknowledgments from clients.

```csharp
public class UpdateDeliveryStatusConsumer : IConsumer<MessageDeliveredCommand>
{
    private readonly IAckHandler _ackHandler;

    public async Task Consume(ConsumeContext<MessageDeliveredCommand> context)
    {
        var chatId = context.Message.ChatId;
        var receiverId = context.Message.ReceiverId;
        var messageId = context.Message.MessageId;
        
        await _ackHandler.HandleAckAsync(
            messageId, 
            senderId, 
            chatId, 
            receiverId, 
            deliveredAt, 
            isSeen: false
        );
    }
}
```

---

### 3. Call Service

Manages WebRTC call sessions in MongoDB.

```csharp
public class CallService : ICallService
{
    private readonly ICallSessionRepository _repository;
    private readonly ILogger<CallService> _logger;

    public async Task<CallSession> CreateSessionAsync(
        string sessionId, 
        string creatorId, 
        string type, 
        string targetUserId, 
        string chatId)
    {
        var session = new CallSession
        {
            CreatorId = creatorId,
            Type = type == "direct" ? SessionType.Direct : SessionType.Group,
            Status = SessionStatus.Ringing,  // Direct calls start as ringing
            Participants = new List<SessionParticipant>
            {
                new SessionParticipant
                {
                    UserId = creatorId,
                    Role = ParticipantRole.Host,
                    Status = ParticipantStatus.Joined
                }
            }
        };

        // Add target for direct calls
        if (type == "direct" && !string.IsNullOrEmpty(targetUserId))
        {
            session.Participants.Add(new SessionParticipant
            {
                UserId = targetUserId,
                Role = ParticipantRole.Member,
                Status = ParticipantStatus.Ringing
            });
        }

        return await _repository.CreateAsync(session);
    }
}
```

---

## Message Processing

### Message Flow

```
Client (WebSocket)
      │
      ▼
Gateway (WebSocket Handler)
      │
      ▼
RabbitMQ: InsertMessageCommand
      │
      ▼
Worker: InsertMessageConsumer
      │
      ├──► MongoDB: Messages Collection
      │
      ▼
RabbitMQ: MessageCreatedEvent
      │
      ▼
BroadcastPreparationWorker
      │
      ▼
RabbitMQ: BroadcastMessageCommand
      │
      ▼
Gateway (Consumer)
      │
      ▼
Target Clients (WebSocket)
```

### Processing Steps

1. **Receive Command**: `InsertMessageConsumer` receives `InsertMessageCommand` from Gateway
2. **Create Entity**: Transform command into `Message` domain entity
3. **Persist**: Save message to MongoDB `Messages` collection
4. **Initialize ACK**: Get or create `ChatGrain` and initialize ACK tracking
5. **Publish Event**: Send `MessageCreatedEvent` to RabbitMQ for broadcasting
6. **Broadcast**: Gateway receives event and pushes to connected clients

---

## Orleans State Management

### Why Orleans?

The ChatSystem uses Orleans for ACK tracking because:

1. **Bitmap Efficiency**: Store ACK status for millions of users in minimal memory
2. **Sub-millisecond Lookups**: In-memory state vs. database queries
3. **Auto-scaling**: Orleans grains are automatically distributed across silos
4. **Persistence**: Automatic state persistence to MongoDB

### Bitmap ACK System

Instead of storing individual ACK records, Orleans uses **bitmaps**:

```csharp
// For a chat with 1000 members:
// Total bitmap size = 1000/8 = 125 bytes

// When message sent:
var bitmapSize = (totalReceivers / 8) + 1;
state.PendingDelivery[msgId] = totalReceivers;
state.DeliveryBitmaps[msgId] = new byte[bitmapSize];

// When member acknowledges:
var index = memberIndex[memberId];
var byteIndex = index / 8;
var bitIndex = index % 8;
bitmap[byteIndex] |= (byte)(1 << bitIndex);
pendingDelivery[msgId]--;

// When all acknowledged:
if (pendingDelivery[msgId] <= 0)
{
    // Publish ACK event to sender
    await _publisher.Publish(new MessageDeliveredAckEvent { ... });
}
```

### Grain Lifecycle

```
Grain Activated
     │
     ▼
Load State from MongoDB (if exists)
     │
     ▼
Process Messages & ACKs (in-memory)
     │
     ▼
Auto-save every 30 seconds
     │
     │
     │ (If idle, Orleans may deactivate)
     ▼
Save State to MongoDB
     ▼
Grain Deactivated
```

---

## ACK Tracking System

### Types of ACKs

| Type | Description | Trigger |
|------|-------------|---------|
| **Delivery** | Message delivered to recipient device | Client receives message |
| **Seen** | User opened/read the message | User views chat |

### ACK Flow

```
Sender sends message
     │
     ▼
Gateway → RabbitMQ: InsertMessageCommand
     │
     ▼
Worker: InsertMessageConsumer
     │
     ├──► MongoDB: Save message
     │
     └──► Orleans: Initialize ACK tracking (pending = all members)
     │
     ▼
Gateway → Client: Push message
     │
     ▼
Client receives → WebSocket: ReceivedACK
     │
     ▼
Gateway → RabbitMQ: MessageDeliveredCommand
     │
     ▼
Worker: UpdateDeliveryStatusConsumer
     │
     └──► Orleans: ReceiveAckAsync (mark bit)
           │
           ▼
           If all ACKed → Publish MessageDeliveredAckEvent
                 │
                 ▼
                 Gateway → Sender: ACK notification
```

### Optimization: Batch ACKs

Clients can send batch acknowledgments to reduce network calls:

```json
{
  "method": "ReceivedACK",
  "params": {
    "chatId": "chat_123",
    "messageIds": ["msg_1", "msg_2", "msg_3"],
    "receivedAt": "2024-01-01T00:00:00Z"
  }
}
```

---

## Call Session Management

### Call Types

1. **Direct Calls**: 1-on-1 voice/video calls
2. **Group Calls**: Multiple participants

### Session States

| State | Description |
|-------|-------------|
| `Created` | Session created, waiting for participants |
| `Ringing` | Direct call waiting for answer |
| `Active` | At least 2 participants joined |
| `Ended` | Call ended |

### Call Flow

```
Caller: Send offer
     │
     ▼
Gateway: OfferMethodHandler
     │
     ├──► SessionStore: Create session (in-memory)
     │
     └──► RabbitMQ: SessionCreatedEvent
           │
           ▼
     Worker: SessionCreatedConsumer
           │
           └──► MongoDB: Save session
                 │
                 ▼
           RabbitMQ: SessionCreatedEvent (response)
                 │
                 ▼
     Gateway → Callee: "offer" signal
           │
           ▼
Callee: Answer
     │
     ▼
Gateway: AnswerMethodHandler
     │
     └──► Gateway → Caller: "answer" signal
           │
           ▼
     Caller & Callee: Exchange ICE candidates
           │
           ▼
     P2P Connection Established
```

### Media State Synchronization

When a participant changes mute/camera state:

```csharp
public async Task UpdateMediaStateAsync(
    string sessionId, 
    string userId, 
    bool isMuted, 
    bool isVideoOn, 
    bool isScreenSharing)
{
    await _repository.UpdateParticipantMediaAsync(
        sessionId, 
        userId, 
        new MediaState 
        { 
            IsMuted = isMuted, 
            IsVideoOn = isVideoOn,
            IsScreenSharing = isScreenSharing 
        }
    );
    
    // Broadcast to all participants
    // ... via RabbitMQ
}
```

---

## Message Flow Diagrams

### Send Message Flow
```
┌────────┐     ┌─────────┐     ┌───────┐     ┌────────┐     ┌─────────┐
│ Client │────►│ Gateway │────►│RabbitMQ│────►│ Worker │────►│ MongoDB │
└────────┘     └─────────┘     └───────┘     └───────┘     └────────┘
                                                                  │
                                                                  ▼
┌────────┐     ┌─────────┐     ┌───────┐     ┌─────────┐     ┌────────┐
│ Client │◄────│ Gateway │◄────│RabbitMQ│◄────│ Broadcast◄────│ Worker │
└────────┘     └─────────┘     └───────┘     │  Prep   │     └────────┘
                                             │ Worker  │
                                             └─────────┘
```

### ACK Flow
```
┌────────┐     ┌─────────┐     ┌───────┐     ┌───────┐     ┌─────────┐
│ Client │────►│ Gateway │────►│RabbitMQ│────►│ Worker │────►│ Orleans │
└────────┘     └─────────┘     └───────┘     └───────┘     │  Grain  │
                                                             └─────────┘
                                                              │
                                                              ▼
                                               (If all ACKed)
                                                              │
                                             ┌────────────────┘
                                             ▼
                                      ┌─────────────┐
                                      │  RabbitMQ   │
                                      └─────────────┘
                                             │
                                             ▼
                                      ┌─────────────┐
                                      │   Gateway   │
                                      └─────────────┘
                                             │
                                             ▼
                                      ┌─────────────┐
                                      │   Sender    │
                                      │   Client    │
                                      └─────────────┘
```

---

## Configuration

### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Orleans": "Warning"
    }
  },
  "ConnectionStrings": {
    "MongoDB": "mongodb://localhost:27017/ChatSystem"
  },
  "MongoDB": {
    "DatabaseName": "ChatSystem"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest",
    "Port": 5672
  },
  " Orleans": {
    "ClusterId": "chat-cluster",
    "ServiceId": "chat-worker",
    "SiloPort": 11111,
    "GatewayPort": 30000
  }
}
```

---

## Dependencies

### NuGet Packages
- `Microsoft.Orleans.Core` - Orleans actor framework
- `Microsoft.Orleans.Persistence.MongoDB` - MongoDB storage provider
- `MassTransit` - Message broker abstraction
- `MongoDB.Driver` - MongoDB client

---

## Background Services

### 1. OrleansSiloHostedService
Starts and manages the Orleans silo.

### 2. MessageProcessingService
Monitors message queue health.

---

## Performance Considerations

### Memory Optimization
- Bitmaps use minimal memory (1 bit per member)
- Orleans grains auto-deactivate when idle
- Periodic state persistence reduces DB load

### Scalability
- Add more Worker instances to scale message processing
- Orleans automatically distributes grains across silos
- MongoDB sharding for large message stores

### Fault Tolerance
- Message persistence before publishing events (at-least-once)
- Orleans state auto-persists every 30 seconds
- Dead letter queues for failed messages

---

## Future Improvements

1. **Redis Grain Storage**: Faster than MongoDB for grain state
2. **Message Deduplication**: Handle duplicate message sends
3. **Read Replicas**: Offload queries to read replicas
4. **Rate Limiting**: Prevent message spam
5. **Push Notifications**: Firebase/APNs for offline users


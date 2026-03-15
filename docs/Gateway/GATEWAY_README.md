# 🚀 Chat Gateway — Architecture & Features

> WebSocket Gateway built with **ASP.NET Core**, **Orleans**, **MassTransit/RabbitMQ**, and **MessagePack**

---

## 📋 Table of Contents

1. [System Overview](#1-system-overview)
2. [WebSocket Connection Flow](#2-websocket-connection-flow)
3. [Message Frame Protocol](#3-message-frame-protocol)
4. [Message Pipeline (Middleware Chain)](#4-message-pipeline-middleware-chain)
5. [MessageContext Pattern](#5-messagecontext-pattern)
6. [Handler System](#6-handler-system)
7. [Orleans Grain Architecture](#7-orleans-grain-architecture)
8. [Broadcast & Fan-Out System](#8-broadcast--fan-out-system)
9. [Bug Fixes Applied](#9-bug-fixes-applied)
10. [Clean Architecture Layers](#10-clean-architecture-layers)
11. [Tech Stack](#11-tech-stack)

---

## 1. System Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                          CLIENTS                                    │
│           📱 Mobile        💻 Web        🖥️ Desktop                │
└──────────────────────┬──────────────────────────────────────────────┘
                       │  WebSocket (Binary / MessagePack)
                       ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      GATEWAY (This Service)                         │
│                                                                     │
│  ┌─────────────┐    ┌──────────────┐    ┌───────────────────────┐   │
│  │  WebSocket  │    │   Message    │    │    Orleans Grains     │   │
│  │ Middleware  │───▶│   Pipeline   │    │  UserGrain/RoomGrain  │  │
│  └─────────────┘    └──────┬───────┘    └───────────────────────┘   │
│                            │                                        │
│                     ┌──────▼───────┐                                │
│                     │   Handlers   │                                │
│                     │ (16 methods) │                                │
│                     └──────┬───────┘                                │
└────────────────────────────┼────────────────────────────────────────┘
                             │
              ┌──────────────┼──────────────┐
              ▼              ▼              ▼
       ┌────────────┐  ┌──────────┐  ┌──────────────┐
       │  RabbitMQ  │  │ MongoDB  │  │   RabbitMQ   │
       │  Publish   │  │ Orleans  │  │  Consumers   │
       │ (Ingress)  │  │  Store   │  │  (Egress)    │
       └────────────┘  └──────────┘  └──────────────┘
```

---

## 2. WebSocket Connection Flow

```
Client                    Middleware              GatewayIngressHandler
  │                           │                           │
  │──── HTTP Upgrade ───────▶ │                           │
  │                           │                           │
  │                    ┌──────▼──────┐                    │
  │                    │ JWT Auth    │                    │
  │                    │ Validate    │                    │
  │                    └──────┬──────┘                    │
  │                           │ ✅ Valid                  │
  │                           │──── AcceptWebSocket ─────▶│
  │                           │                           │
  │                           │              ┌────────────▼────────────┐
  │                           │              │  Create MessageContext  │
  │                           │              │  ┌────────────────────┐ │
  │                           │              │  │ ConnectionId (GUID)│ │
  │                           │              │  │ UserId             │ │
  │                           │              │  │ Socket             │ │
  │                           │              │  │ FrameWriter        │ │
  │                           │              │  │ FrameReader        │ │
  │                           │              │  │ ConnectedAt        │ │
  │                           │              │  │ MessagesReceived   │ │
  │                           │              └──┴────────────────────┘ │
  │                           │              └────────────┬────────────┘
  │                           │                           │
  │                           │              ┌────────────▼────────────┐
  │                           │              │  Register in:           │
  │                           │              │  • LocalWebSocketRegistry│
  │                           │              │  • UserGrain (Orleans)  │
  │                           │              └────────────┬────────────┘
  │                           │                           │
  │◀──── "connected" event ───────────────────────────────│
  │      { connectionId, timestamp }                      │
  │                           │                           │
  │════ Binary Frames ════════════════════════════════════│
  │                           │              ┌────────────▼────────────┐
  │                           │              │  FrameReader (Pipe)     │
  │                           │              │  ReadFramesAsync()      │
  │                           │              │  IAsyncEnumerable       │
  │                           │              └────────────┬────────────┘
  │                           │                           │
  │                           │              ┌────────────▼────────────┐
  │                           │              │   Message Pipeline      │
  │                           │              │   (4 middlewares)       │
  │                           │              └─────────────────────────┘
  │                           │                           │
  │──── Close Frame ─────────────────────────────────────▶│
  │                           │              ┌────────────▼────────────┐
  │                           │              │  Disconnect:            │
  │                           │              │  • LocalWebSocketRegistry│
  │                           │              │  • UserGrain (Orleans)  │
  │                           │              │  • FrameReader.Dispose  │
  │                           │              └─────────────────────────┘
```

---

## 3. Message Frame Protocol

كل message بتتلف في **Frame** قبل ما تتبعت عبر الـ WebSocket:

```
 Byte Layout:
 ┌─────────────────────────────────────────────────────────┐
 │  0    1    2    3    │  4   │  5 ... N                  │
 │  ─────────────────── │ ─── │ ──────────────────────     │
 │  Payload Length      │Type │ Payload (MessagePack)      │
 │  (4 bytes Big-Endian)│(1b) │ (variable length)          │
 └─────────────────────────────────────────────────────────┘
        │                 │
        │                 └── Frame Types:
        │                      0x01 = Message
        │                      0x02 = Response
        │                      0x03 = Ping
        │                      0x04 = Pong
        │                      0x05 = Close
        │                      0xFF = Error
        │
        └── Header = 5 bytes fixed

 Example — "NewMessage" frame:
 ┌──────┬──────┬──────┬──────┬──────┬─────────────────────┐
 │  00  │  00  │  00  │  4A  │  01  │  [MessagePack data] │
 └──────┴──────┴──────┴──────┴──────┴─────────────────────┘
   Length = 74 bytes            Type = Message
```

### FrameReader — Zero-Copy Pipeline
```
Socket.ReceiveAsync()
        │
        ▼
   System.IO.Pipe  ◀── Background Task (ReadFromSocketAsync)
        │
        ▼
   TryReadFrame()
   ┌─────────────────────────────────────────────────────┐
   │  1. Read Header (5 bytes)                           │
   │     → Extract Length + FrameType                    │
   │  2. Rent buffer from ArrayPool                      │
   │  3. Accumulate payload bytes                        │
   │  4. When complete → payload.ToArray() ✅           │
   │    (independent copy — no corruption between frames)│
   └─────────────────────────────────────────────────────┘
        │
        ▼
   yield MessageFrame  →  IAsyncEnumerable<MessageFrame>
```

---

## 4. Message Pipeline (Middleware Chain)

### Architecture

```
                    Frame.Payload (ReadOnlyMemory<byte>)
                              │
                              ▼
          ┌───────────────────────────────────────┐
          │         MessagePipeline               │
          │   (built once at startup via DI)      │
          └───────────────┬───────────────────────┘
                          │
          ┌───────────────▼───────────────────────┐
          │      1. MetricsMiddleware             │
          │   • Starts Activity (Distributed      │
          │     Tracing / OpenTelemetry)          │
          │   • Measures processing time          │
          │   • Records success/error metrics     │
          │   • Wraps ALL other middlewares       │
          └───────────────┬───────────────────────┘
                          │
          ┌───────────────▼───────────────────────┐
          │      2. RateLimitMiddleware           │
          │   • TokenBucket algorithm             │
          │   • 100 req/sec per userId            │
          │   • Exceeded → SendErrorAsync()       │
          │     and STOP pipeline                 │
          │   • OK → continue ↓                   │
          └───────────────┬───────────────────────┘
                          │
          ┌───────────────▼───────────────────────┐
          │      3. DecompressionMiddleware       │
          │   • Checks Gzip magic bytes (1F 8B)   │
          │   • Not compressed → pass through     │
          │     (zero overhead)                   │
          │   • Compressed → Gzip decompress      │
          │     → pass new payload ↓              │
          └───────────────┬───────────────────────┘
                          │
          ┌───────────────▼───────────────────────┐
          │      4. DispatchMiddleware            │
          │   • Deserialize → MessageEnvelope     │
          │   • Validate (Method not empty)       │
          │   • Invalid → SendErrorAsync() STOP   │
          │   • Valid → MethodDispatcher          │
          └───────────────┬───────────────────────┘
                          │
          ┌───────────────▼───────────────────────┐
          │         MethodDispatcher              │
          │   Dictionary<string, IMethodHandler>  │
          │   Lookup by method name (O(1))        │
          └───────────────┬───────────────────────┘
                          │
          ┌───────────────▼───────────────────────┐
          │       Concrete Handler                │
          │  e.g. NewMessageMethodHandler         │
          │       JoinCallMethodHandler           │
          └───────────────────────────────────────┘
```

### كيف بيتبنى الـ Pipeline

```csharp
// DI Registration — الترتيب مهم:
services.AddSingleton<IMessageMiddleware, MetricsMiddleware>();      // 1st
services.AddSingleton<IMessageMiddleware, RateLimitMiddleware>();    // 2nd
services.AddSingleton<IMessageMiddleware, DecompressionMiddleware>(); // 3rd
services.AddSingleton<IMessageMiddleware, DispatchMiddleware>();     // 4th
services.AddSingleton<IMessagePipeline, MessagePipeline>();

// MessagePipeline.cs — بيبني الـ chain مرة واحدة:
_pipeline = middlewares
    .Reverse()
    .Aggregate(terminal, (next, middleware) =>
        (ctx, payload, ct) => middleware.InvokeAsync(ctx, payload, next, ct));
```

### إضافة Middleware جديد
```csharp
// مثلاً — Logging Middleware
public class MessageLoggingMiddleware : IMessageMiddleware
{
    public async Task InvokeAsync(
        MessageContext context,
        ReadOnlyMemory<byte> payload,
        MessageMiddlewareDelegate next,
        CancellationToken ct)
    {
        _logger.LogInformation("Message received | user={UserId} | size={Size}",
            context.UserId, payload.Length);

        await next(context, payload, ct); // ← كمّل للـ middleware الجاي
    }
}

// في InfrastructureDep.cs — سطر واحد بس:
services.AddSingleton<IMessageMiddleware, MessageLoggingMiddleware>();
```

---

## 5. MessageContext Pattern

### قبل وبعد

```
╔══════════════════════════════════════════════════════════════════╗
║  قبل — userId + socket بيتمرروا منفصلين في كل حاجة            ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  GatewayIngressHandler(userId, socket)                           ║
║          ↓                                                       ║
║  Dispatcher(userId, method, params, socket)                      ║
║          ↓                                                       ║
║  Handler.Handle(userId, data, socket)                            ║
║          ↓                                                       ║
║  HandleAsync(userId, T, socket)                                  ║
║                                                                  ║
╠══════════════════════════════════════════════════════════════════╣
║  بعد — MessageContext بيتمرر في كل الـ chain                   ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  GatewayIngressHandler(userId, socket)                           ║ 
║          ↓  ← CREATE MessageContext هنا بس                        ║
║  Pipeline.ExecuteAsync(context, payload)                         ║
║          ↓                                                       ║
║  Handler.Handle(context, data)                                   ║
║          ↓                                                       ║
║  HandleAsync(context, T)                                         ║
║          ↓                                                       ║
║  context.UserId                 ← بدل string userId               ║
║  context.SendResponseAsync()    ← بدل socket مباشرةً                 ║
║  context.SendErrorAsync()       ← structured error               ║
║  context.ConnectionId           ← للـ logging                      ║
║  context.MessagesReceived       ← metrics                        ║
║                                                                  ║
╚══════════════════════════════════════════════════════════════════╝
```

### ما بيحمله الـ MessageContext

```
┌──────────────────────────────────────────────────────────┐
│                    MessageContext                        │
├──────────────────────────────────────────────────────────┤
│  Identity                                                │
│  ├── ConnectionId : string (GUID)                        │
│  └── UserId       : string                               │
├──────────────────────────────────────────────────────────┤
│  Transport                                               │
│  ├── Socket  : WebSocket                                 │
│  ├── Writer  : FrameWriter   ← للإرسال                      │
│  └── Reader  : FrameReader   ← للاستقبال                     │
├──────────────────────────────────────────────────────────┤
│  State                                                   │
│  ├── ConnectionState : Connected/Closing/Disconnected    │
│  ├── ConnectedAt    : DateTime                           │
│  └── LastActivityAt : DateTime                           │
├──────────────────────────────────────────────────────────┤
│  Metrics (Thread-Safe via Interlocked)                   │
│  ├── MessagesReceived : long                             │
│  └── MessagesSent     : long                             │
├──────────────────────────────────────────────────────────┤
│  Send API                                                │
│  ├── SendAsync<T>()        ← object → serialize → send   │
│  ├── SendRawAsync()        ← pre-serialized bytes        │
│  ├── SendResponseAsync()   ← structured response         │
│  ├── SendErrorAsync()      ← structured error            │
│  ├── SendPingAsync()                                     │
│  └── SendPongAsync()                                     │
└──────────────────────────────────────────────────────────┘
```

---

## 6. Handler System

### 16 Handlers — 5 Categories

```
IMethodHandler
      │
      ├── BaseMethodHandler<T>  (abstract)
      │         • Deserialize payload → T
      │         • Error handling for bad payload
      │         • Calls HandleAsync(context, T)
      │
      ├── 📨 Message (4 handlers)
      │   ├── NewMessageMethodHandler          "NewMessage"
      │   ├── MessageReceivedAckMethodHandler  "ReceivedACK"
      │   ├── MessageSeenAckMethodHandler      "SeenACKBatch"
      │   └── ReceivedAckBatchMethodHandler    "ReceivedACKBatch"
      │
      ├── 📞 Call / WebRTC (8 handlers)
      │   ├── OfferMethodHandler               "offer"
      │   ├── AnswerMethodHandler              "answer"
      │   ├── IceCandidateMethodHandler        "ice_candidate"
      │   ├── JoinCallMethodHandler            "join_call"
      │   ├── LeaveCallHandler                 "leave_call"
      │   ├── GroupSignalMethodHandler         "group_signal"
      │   ├── MediaStateHandler                "media_state"
      │   └── CreateGroupCallHandler           "create_group_call"
      │
      ├── 👤 State (2 handlers)
      │   ├── UserStateMethodHandler           "UserState"
      │   └── GroupStateMethodHandler          "GroupState"
      │
      ├── 🔄 Sync (1 handler)
      │   └── SyncUserAckMethodHanlder         "SyncUserShotAck"
      │
      └── 📸 Snapshots (1 handler)
          └── ReceivedSnapAckBatchMethodHandler "ReceivedSnapACKBatch"
```

### WebRTC Call Flow
```
Caller                   Gateway                    Callee
  │                          │                         │
  │── offer ───────────────▶ │                         │
  │  (CreateGroupCallHandler)│                        │
  │                         │──── incoming_call ─────▶│
  │                         │    (RabbitMQ egress)    │
  │                         │                         │
  │                         │◀── join_call ───────────│
  │                         │  (JoinCallMethodHandler) │
  │◀── call_answered ───────│                         │
  │                         │                         │
  │── answer ──────────────▶│                         │
  │  (AnswerMethodHandler)  │──── answer ────────────▶│
  │                         │                         │
  │══ ice_candidate ════════════════════════════════▶ │
  │ ◀═ ice_candidate ═══════════════════════════════  │
  │                         │                         │
  │                    [WebRTC P2P]                    │
  │◀══════════════════════════════════════════════════▶│
  │                                                   │
  │── leave_call ──────────▶│                         │
  │  (LeaveCallHandler)     │──── call_ended ─────────▶│
  │◀── call_ended ──────────│                         │
```

---

## 7. Orleans Grain Architecture

### الـ Grains وما بيعملوه

```
┌─────────────────────────────────────────────────────────────────┐
│                     Orleans Silo                                 │
│                                                                  │
│  ┌──────────────────────┐    ┌───────────────────────────────┐  │
│  │    UserGrain         │    │         RoomGrain             │  │
│  │  (per userId)        │    │    (per chatId/sessionId)     │  │
│  │                      │    │                               │  │
│  │  Persistent State:   │    │  Persistent State:            │  │
│  │  ├── IsOnline: bool  │    │  └── Members: HashSet<string> │  │
│  │  └── LastSeen: DateTime│  │                               │  │
│  │                      │    │  In-Memory Cache (TTL 30s):   │  │
│  │  In-Memory (transient):│  │  └── GroupPresence            │  │
│  │  └── ActiveConnections │  │                               │  │
│  │      HashSet<string>  │    │  Methods:                     │  │
│  │                      │    │  ├── JoinAsync(userId)         │  │
│  │  DB Write Policy:    │    │  ├── LeaveAsync(userId)        │  │
│  │  Write ONLY when:    │    │  ├── GetMembersAsync()         │  │
│  │  offline→online      │    │  ├── GetPresenceAsync() ←cache │  │
│  │  online→offline      │    │  └── InvalidatePresenceCache() │  │
│  └──────────────────────┘    └───────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### UserGrain — DB Write Optimization

```
قبل: كل connection → DB Write
─────────────────────────────────────
User فاتح Tab 1  → WriteStateAsync()  ✗
User فاتح Tab 2  → WriteStateAsync()  ✗
User فاتح Tab 3  → WriteStateAsync()  ✗
User سكّر Tab 1  → WriteStateAsync()  ✗
User سكّر Tab 2  → WriteStateAsync()  ✗
User سكّر Tab 3  → WriteStateAsync()  ✗

1000 user × 3 tabs × reconnect = 6000 DB writes/min

بعد: بس عند state transition
─────────────────────────────────────
User فاتح Tab 1  → WriteStateAsync()  ✅ (offline→online)
User فاتح Tab 2  → (skip)
User فاتح Tab 3  → (skip)
User سكّر Tab 1  → (skip)
User سكّر Tab 2  → (skip)
User سكّر Tab 3  → WriteStateAsync()  ✅ (online→offline)

1000 user × reconnect = 2 DB writes/session
```

### RoomGrain — N+1 Cache

```
قبل: كل GetPresenceAsync على Room بـ 500 member = 500 Orleans calls
──────────────────────────────────────────────────────────────────
                  RoomGrain.GetPresenceAsync()
                           │
            ┌──────────────┼──────────────┐
            ▼              ▼              ▼
      UserGrain[1]   UserGrain[2]  ... UserGrain[500]
      .IsOnline()    .IsOnline()       .IsOnline()
            └──────────────┼──────────────┘
                      500 network calls!

بعد: Cache بـ TTL 30 ثانية
──────────────────────────────────────────────────────────────────
طلب 1 → Cache MISS → 500 calls → نتيجة → تتحفظ 30 ثانية
طلب 2 → Cache HIT  → 0 calls   ← إرجاع النتيجة فوراً ✅
طلب 3 → Cache HIT  → 0 calls   ✅
...
بعد 30 ثانية → Cache MISS → 500 calls → نتيجة جديدة → ...

+ Timeout 5 ثواني على الـ fan-out (مش بيـblock للأبد)
+ InvalidatePresenceCacheAsync() عند Join/Leave (فوري)
```

---

## 8. Broadcast & Fan-Out System

```
Message من RabbitMQ Consumer
            │
            ▼
  OutgoingMessageService
  (SendToRoomAsync / SendToUserAsync)
            │
            ▼  يجيب الـ contexts من الـ buffer
  FanOutResolverManager
  ┌──────────────────────────────────────────┐
  │  1. RoomGrain.GetMembersAsync()          │
  │     → List<userId>                       │
  │  2. لكل userId:                         │
  │     LocalWebSocketRegistry               │
  │     .GetUserContexts(userId)             │
  │     → List<MessageContext>               │
  └──────────────────────────┬───────────────┘
                             │
                             ▼
                    BroadcastManager
            ┌────────────────────────────────┐
            │  payload = Serialize(message)   │
            │  (مرة واحدة بس ✅)             │
            │                                │
            │  Parallel.ForEachAsync()       │
            │  ┌──────┐ ┌──────┐ ┌──────┐  │
            │  │ctx[0]│ │ctx[1]│ │ctx[N]│  │
            │  │      │ │      │ │      │  │
            │  │Send  │ │Send  │ │Send  │  │
            │  │Raw() │ │Raw() │ │Raw() │  │
            │  └──────┘ └──────┘ └──────┘  │
            │  نفس الـ ReadOnlyMemory       │
            │  مفيش N allocations ✅         │
            └────────────────────────────────┘
```

### Egress Consumers (RabbitMQ → WebSocket)

```
RabbitMQ Queues:
┌──────────────────────────┬──────────────────────────────────────┐
│ Queue Name               │ Consumer → Action                    │
├──────────────────────────┼──────────────────────────────────────┤
│ WebSocket-Engress-queue  │ BroadcastMessageConsumer             │
│                          │ → SendToRoomAsync (new_message)      │
├──────────────────────────┼──────────────────────────────────────┤
│ WebSocket-Ack-Store-queue│ AckStoreConsumer                     │
│                          │ → Store delivery receipt             │
├──────────────────────────┼──────────────────────────────────────┤
│ WebSocket-Ack-Seen-queue │ SeenAckMessageConsumer               │
│                          │ → SendToUserAsync (message_seen)     │
├──────────────────────────┼──────────────────────────────────────┤
│ WebSocket-Ack-Delivered  │ AckDeliveredConsumer                 │
│                          │ → SendToUserAsync (message_delivered)│
├──────────────────────────┼──────────────────────────────────────┤
│ WebSocket-New-Chat-queue │ NewChatConsumer                      │
│                          │ → RegisterInGroup + SendToRoom       │
├──────────────────────────┼──────────────────────────────────────┤
│ WebSocket-Story-Broadcast│ StoryBroadcastConsumer               │
│                          │ → SendToUsersAsync (new_story)       │
└──────────────────────────┴──────────────────────────────────────┘
```

---

## 9. Bug Fixes Applied

### Fix 1 — Memory Corruption في FrameReader 🔴

```
قبل:
┌────────────────────────────────────────────────────────┐
│  _rentedBuffer: [  Frame 1 data  ]                    │
│                        ↑                              │
│  yield return frame ───┘  (يشاور على نفس الـ buffer) │
│                                                        │
│  Caller بيشتغل على frame...                          │
│  في نفس الوقت!                                        │
│  Reader يكتب Frame 2 فوق الـ buffer                   │
│                 ↓                                      │
│           CORRUPTION ❌                               │
└────────────────────────────────────────────────────────┘

بعد:
┌────────────────────────────────────────────────────────┐
│  _rentedBuffer: [  Frame 1 data  ]                    │
│                                                        │
│  payloadCopy = _rentedBuffer.AsSpan().ToArray()       │
│  ↓                                                     │
│  payloadCopy: [  Frame 1 data  ]  ← نسخة مستقلة ✅   │
│                                                        │
│  yield return frame (بالـ payloadCopy)                │
│  _rentedBuffer حر يتستخدم للـ frame الجاي            │
└────────────────────────────────────────────────────────┘
```

### Fix 2 — FrameReader Memory Leak 🔴

```
قبل:                              بعد:
var reader = new FrameReader();   await using var reader = new FrameReader();
// Background task شغالة         // DisposeAsync() بيوقف الـ task
// ArrayPool buffers ضايعة        // ArrayPool buffers بترجع ✅
// Leak مع كل connection ❌
```

### Fix 3 — Double Serialization في Broadcast 🔴

```
قبل:
OutgoingMessageService:
  payload = Serialize(message)         → byte[] ✅

BroadcastManager:
  context.SendAsync(message.ToArray()) → Serialize(byte[]) ❌
                                         (bytes داخل bytes!)
  Client بيستقبل garbage 💥

بعد:
OutgoingMessageService:
  payload = Serialize(message)         → byte[] ✅

BroadcastManager:
  context.SendRawAsync(payload)        → frame header فقط ✅
  Client بيستقبل data صح ✅

+ مفيش ToArray() per connection
  (نفس ReadOnlyMemory لكل الـ contexts)
```

### Fix 4 — Thread Safety في Metrics 🟡

```
قبل:                              بعد:
public long MessagesReceived      private long _messagesReceived;
    { get; set; }                 public long MessagesReceived =>
MessagesReceived++;                   Interlocked.Read(ref _messagesReceived);
// Race condition على 64-bit ❌   Interlocked.Increment(ref _messagesReceived);
                                  // Atomic ✅
```

---

## 10. Clean Architecture Layers

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Application Layer                             │
│                                                                      │
│  Abstractions/                    Handlers/                         │
│  ├── Pipeline/                    ├── Message/ (4)                  │
│  │   ├── IMessageMiddleware       ├── Call/ (8)                     │
│  │   └── IMessagePipeline         ├── State/ (2)                    │
│  ├── Handler/                     ├── Sync/ (1)                     │
│  │   ├── IMethodHandler           └── Snapshots/ (1)               │
│  │   ├── BaseMethodHandler<T>                                       │
│  │   └── IMethodDispatcher        Messaging/                        │
│  ├── Connection/                  ├── MessageContext                │
│  │   ├── IConnectionServices      ├── MessageFrame                  │
│  │   ├── IWebSocketRegistry       ├── MessageEnvelope               │
│  │   └── Grains/                  ├── FrameReader                   │
│  │       ├── IUserGrain           └── FrameWriter                   │
│  │       └── IRoomGrain                                             │
│  └── [Broadcast, Metrics,         Serialization/                   │
│        RateLimiting, ...]         └── MessageSerializer             │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                      Infrastructure Layer                            │
│                                                                      │
│  Pipeline/                        Grains/                           │
│  ├── MessagePipeline              ├── UserGrain                     │
│  └── Middlewares/                 └── RoomGrain                     │
│      ├── MetricsMiddleware                                          │
│      ├── RateLimitMiddleware       Services/                        │
│      ├── DecompressionMiddleware   ├── Broadcast/                   │
│      └── DispatchMiddleware        │   ├── BroadcastManager         │
│                                    │   ├── FanOutResolverManager     │
│  Connection/                       │   └── OutgoingMessageService    │
│  ├── ConnectionServices            ├── Auth/                        │
│  └── LocalWebSocketRegistry        ├── Publisher/ (RabbitMQ)        │
│                                    └── Session/                     │
│  WebSocketHandler/                                                  │
│  ├── Ingress/                     RateLimiting/                     │
│  │   └── GatewayIngressHandler    └── TokenBucketRateLimiter        │
│  ├── Dispatcher/                                                    │
│  │   └── MethodDispatcher         Compression/                     │
│  └── Egress/Consumers/ (6)        └── GzipMessageCompressor        │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                         Gateway Layer                                │
│  (Entry Point)                                                       │
│                                                                      │
│  Program.cs                       Middleware/                       │
│  ├── AddDbInjection               └── WebSocketMiddleware           │
│  ├── AddAuthentcationDep               ├── JWT Validation           │
│  ├── AddMassRabbitMqDep                ├── AcceptWebSocket          │
│  ├── AddInfraDep                       └── → GatewayIngressHandler  │
│  └── UseOrleans                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 11. Tech Stack

```
┌────────────────────────────────────────────────────────────────┐
│  Runtime      │  .NET 9 / ASP.NET Core                        │
├────────────────────────────────────────────────────────────────┤
│  Protocol     │  WebSocket (Binary) + Custom Frame Protocol   │
├────────────────────────────────────────────────────────────────┤
│  Serialization│  MessagePack (binary, fast, compact)          │
├────────────────────────────────────────────────────────────────┤
│  Distributed  │  Microsoft Orleans (Virtual Actor Model)      │
│  State        │  MongoDB Storage Provider                     │
├────────────────────────────────────────────────────────────────┤
│  Messaging    │  MassTransit + RabbitMQ                       │
│               │  6 queues (ingress publish + egress consume)  │
├────────────────────────────────────────────────────────────────┤
│  Database     │  MongoDB                                      │
├────────────────────────────────────────────────────────────────┤
│  Auth         │  JWT Bearer                                   │
├────────────────────────────────────────────────────────────────┤
│  Observability│  OpenTelemetry (Metrics + ActivitySource)     │
├────────────────────────────────────────────────────────────────┤
│  Performance  │  System.IO.Pipelines, ArrayPool, Parallel.   │
│               │  ForEachAsync, ReadOnlyMemory (zero-copy)     │
└────────────────────────────────────────────────────────────────┘
```

---

*Last updated: March 2026*

# BroadcastPreparationWorker - Comprehensive Documentation

## Overview
The **BroadcastPreparationWorker** is a specialized worker service in the ChatSystem that acts as an event preprocessing layer. It receives events from the Worker service, processes them through a pipeline of steps (side effects), and dispatches the prepared data to downstream consumers (primarily the Gateway). This service implements the **Pipeline Pattern** for flexible and extensible event processing.

## Technology Stack
- **Framework**: .NET 8.0/9.0
- **Architecture**: Event Pipeline (Pipeline Pattern)
- **Messaging**: MassTransit with RabbitMQ
- **Database**: MongoDB
- **Pattern**: Side Effect Pattern

---

## Table of Contents
1. [Project Structure](#project-structure)
2. [Core Concepts](#core-concepts)
3. [Event Pipeline Architecture](#event-pipeline-architecture)
4. [Pipeline Steps](#pipeline-steps)
5. [Event Flow](#event-flow)
6. [Message Flow Diagram](#message-flow-diagram)
7. [Configuration](#configuration)
8. [Error Handling](#error-handling)
9. [Dependencies](#dependencies)

---

## Project Structure

```
BroadcastPreparationWorker/
├── BroadcastPreparationWorker/          # Main Project
│   ├── Program.cs                        # Entry point
│   ├── appsettings.json
│   └── Dockerfile
├── Application/                          # Application Layer
│   ├── Abstractions/
│   │   └── EventPipeline/               # Pipeline abstractions
│   │       └── IEventPipelineStep.cs
│   └── Dtos/                           # Data Transfer Objects
├── Domain/                                # Domain Layer
│   └── Models/
└── Infrastructure/                       # Infrastructure Layer
    ├── Consumers/
    │   └── EventConsumer.cs              # Generic event consumer
    ├── EventPipeline/
    │   └── EventPipeline.cs              # Pipeline executor
    └── Handler/
        └── EventHandler/
            └── MessageStored/
                ├── Steps/                 # Sequential processing steps
                │   └── AckStoreStep.cs   # ACK storage step
                └── SideEffect/            # Side effect steps
                    ├── BroadcastStep.cs   # Message broadcast preparation
                    └── SnapshotUpdateStep.cs # Chat snapshot update
```

---

## Core Concepts

### 1. Event Pipeline Pattern
The BroadcastPreparationWorker uses a **Pipeline Pattern** to process events. Each event goes through a series of steps:

```
Event → Step 1 → Step 2 → Step 3 → ... → Complete
```

**Key Features:**
- **Sequential Processing**: Steps execute in order
- **Side Effects**: Each step can trigger additional actions
- **Async Execution**: Non-blocking pipeline execution
- **Extensibility**: Easy to add new steps

### 2. Side Effects
The pipeline implements a side-effect pattern where:
- **Synchronous Steps**: Execute and pass control to the next step
- **Fire-and-Forget Steps**: Execute async tasks without blocking the pipeline

### 3. Generic Event Consumer
The service uses a generic event consumer that routes any event type to the appropriate pipeline:

```csharp
public class EventConsumer<TEvent> : IConsumer<TEvent> where TEvent : class
{
    private readonly EventPipeline<TEvent> _pipeline;

    public Task Consume(ConsumeContext<TEvent> context)
        => _pipeline.ExecuteAsync(context.Message);
}
```

---

## Event Pipeline Architecture

### Pipeline Execution Flow

```
RabbitMQ Event
      │
      ▼
EventConsumer<TEvent>
      │
      ▼
EventPipeline<TEvent>
      │
      ├──► Step 1 (Execute) ──► Step 2 (Execute) ──► Step 3 (Execute)
      │         │                    │                    │
      │         ▼                    ▼                    ▼
      │      Business Logic     Business Logic      Business Logic
      │         │                    │                    │
      │         ▼                    ▼                    ▼
      │      Next()              Next()               Next()
      │         │                    │                    │
      └─────────┴────────────────────┴────────────────────┘
                         │
                         ▼
                   Pipeline Complete
```

### EventPipeline Implementation

```csharp
public class EventPipeline<TEvent>
{
    private readonly IReadOnlyList<IEventPipelineStep<TEvent>> _steps;

    public Task ExecuteAsync(TEvent evt)
    {
        var index = -1;

        Task Next()
        {
            index++;
            if (index < _steps.Count)
                return _steps[index].HandleAsync(evt, Next);

            return Task.CompletedTask;
        }

        return Next();
    }
}
```

---

## Pipeline Steps

### 1. AckStoreStep
**Purpose**: Store acknowledgment event for the message sender.

```csharp
public class AckStoreStep : IEventPipelineStep<MessageCreatedEvent>
{
    private readonly IMessagePublisher _publish;

    public async Task HandleAsync(MessageCreatedEvent evt, Func<Task> next)
    {
        // First: Publish ACK event to notify sender
        await _publish.PublishAsync(new MessageStoredAckEvent
        {
            MessageId = evt.MessageId,
            SenderId = evt.SenderId,
            ChatId = evt.ChatId,
            ClientMessageId = evt.ClientMessageId,
        });

        // Then: Continue to next step
        await next();
    }
}
```

**Flow:**
1. Publish `MessageStoredAckEvent` to RabbitMQ
2. Pass control to next step

---

### 2. BroadcastStep (Side Effect)
**Purpose**: Prepare message for broadcast to chat participants.

```csharp
public class BroadcastStep : IEventPipelineStep<MessageCreatedEvent>
{
    private readonly IMessagePublisher _publish;
    private readonly IUserRepositoryQuerey _userRepository;

    public async Task HandleAsync(MessageCreatedEvent evt, Func<Task> next)
    {
        // First: Continue pipeline execution
        await next();

        // Side Effect: Prepare broadcast (Fire-and-Forget)
        _ = Task.Run(async () =>
        {
            try
            {
                // Get sender info
                var userinfo = await _userRepository.GetUserInfo(ObjectId.Parse(evt.SenderId));

                // Create broadcast command
                var broadcastCommand = new BroadcastMessageCommand()
                {
                    ChatId = evt.ChatId,
                    SenderName = userinfo?.UserName ?? "Unknown",
                    Content = evt.Content,
                    MessageId = evt.MessageId,
                    MessageType = evt.MessageType,
                    SenderId = evt.SenderId,
                };

                // Publish for Gateway to consume
                await _publish.PublishAsync(broadcastCommand);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BroadcastStep failed");
            }
        });
    }
}
```

**Flow:**
1. Execute `next()` to continue pipeline
2. **Side Effect**: In background task:
   - Fetch sender information from MongoDB
   - Create `BroadcastMessageCommand`
   - Publish to RabbitMQ for Gateway

---

### 3. SnapshotUpdateStep (Side Effect)
**Purpose**: Update chat snapshots for all participants.

```csharp
public class SnapshotUpdateStep : IEventPipelineStep<MessageCreatedEvent>
{
    private readonly IMessagePublisher _publish;
    private readonly IUserRepositoryQuerey _userRepository;

    public async Task HandleAsync(MessageCreatedEvent evt, Func<Task> next)
    {
        // First: Continue pipeline execution
        await next();

        // Side Effect: Update snapshots (Fire-and-Forget)
        _ = Task.Run(async () =>
        {
            try
            {
                // Get sender info
                var userinfo = await _userRepository.GetUserInfo(ObjectId.Parse(evt.SenderId));

                // Create snapshot update command
                var updateCommand = new UpdateChatSnapshotCommand()
                {
                    MessageId = evt.MessageId,
                    SenderId = evt.SenderId,
                    ChatId = evt.ChatId,
                    Content = evt.Content,
                    SenderName = userinfo.UserName,
                    SentAt = evt.SentAt,
                };

                // Publish for snapshot consumers
                await _publish.PublishAsync(updateCommand);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SnapshotUpdateStep failed: {ex.Message}");
            }
        });
    }
}
```

**Flow:**
1. Execute `next()` to continue pipeline
2. **Side Effect**: In background task:
   - Fetch sender information
   - Create `UpdateChatSnapshotCommand`
   - Publish to RabbitMQ

---

## Event Flow

### Message Created Flow

```
Worker Service
     │
     ▼
RabbitMQ: MessageCreatedEvent
     │
     ▼
BroadcastPreparationWorker: EventConsumer<MessageCreatedEvent>
     │
     ▼
EventPipeline<MessageCreatedEvent>
     │
     ├── Step 1: AckStoreStep
     │     │
     │     └──► RabbitMQ: MessageStoredAckEvent
     │               │
     │               └──► Worker Service: Update Orleans ACK
     │
     ├── Step 2: BroadcastStep (Side Effect)
     │     │
     │     └──► RabbitMQ: BroadcastMessageCommand
     │               │
     │               └──► Gateway: Push to clients
     │
     └── Step 3: SnapshotUpdateStep (Side Effect)
           │
           └──► RabbitMQ: UpdateChatSnapshotCommand
                     │
                     └──► Worker: Update chat snapshots
```

---

## Message Flow Diagram

```
┌─────────┐     ┌───────┐     ┌─────────────────────────┐     ┌─────────┐
│ Worker  │────►│RabbitMQ│────►│BroadcastPreparation     │────►│RabbitMQ │
│ Service │     │       │     │Worker                    │     │         │
└─────────┘     └───────┘     └─────────────────────────┘     └───────┘
                                           │                        │
                                           │ 1. AckStoreStep        │
                                           │────────────────────────│
                                           │                        │
                                           │ 2. BroadcastStep      │
                                           │────────────────────────│
                                           │                        │
                                           │ 3. SnapshotUpdateStep │
                                           │────────────────────────│
                                           ▼                        ▼
                                    ┌───────────┐           ┌───────────┐
                                    │  Message  │           │ Broadcast │
                                    │   Stored  │           │  Message  │
                                    │    Ack    │           │ Command   │
                                    └───────────┘           └───────────┘
                                           │                        │
                                           ▼                        ▼
                                    ┌───────────┐           ┌───────────┐
                                    │  Worker   │           │  Gateway  │
                                    │  Service  │           │           │
                                    └───────────┘           └───────────┘


┌─────────────┐     ┌──────────────────┐     ┌──────────┐
│   RabbitMQ  │────►│ SnapshotUpdate   │────►│  Worker  │
│             │     │    Command       │     │  Service │
└─────────────┘     └──────────────────┘     └──────────┘
```

---

## Configuration

### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "ConnectionStrings": {
    "MongoDB": "mongodb://localhost:27017/ChatSystem"
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

## Error Handling

### Try-Catch in Side Effects
The pipeline steps use try-catch blocks to handle errors gracefully:

```csharp
_ = Task.Run(async () =>
{
    try
    {
        // Process event
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "BroadcastStep failed for MessageId: {MessageId}", evt.MessageId);
    }
});
```

### Error Recovery
- **AckStoreStep**: Errors are logged but don't break the pipeline
- **BroadcastStep**: Uses fire-and-forgit pattern, errors are logged
- **SnapshotUpdateStep**: Errors are caught and logged to console

---

## Dependencies

### NuGet Packages
- `MassTransit` - Message broker abstraction
- `RabbitMQ.Client` - RabbitMQ client
- `Microsoft.Extensions.Logging` - Logging abstraction
- `MongoDB.Driver` - MongoDB client

---

## Event Types

### Input Events
| Event | Source | Description |
|-------|--------|-------------|
| `MessageCreatedEvent` | Worker Service | New message created in chat |

### Output Events
| Event | Destination | Description |
|-------|-------------|-------------|
| `MessageStoredAckEvent` | Worker Service | ACK that message is stored |
| `BroadcastMessageCommand` | Gateway | Push message to clients |
| `UpdateChatSnapshotCommand` | Worker Service | Update chat list snapshots |

---

## Performance Considerations

### Async Side Effects
- Side effect steps use `Task.Run()` for background processing
- Pipeline continues immediately without waiting for side effects
- Reduces latency for the main event flow

### Scalability
- Add more worker instances to handle high message throughput
- Each instance processes events independently
- No shared state between instances

### Fault Tolerance
- Errors in side effects don't crash the pipeline
- Each side effect is isolated in its own try-catch
- Failed side effects can be retried via RabbitMQ dead-letter queues

---

## Future Improvements

1. **Retry Mechanism**: Add retry logic for failed side effects
2. **Dead Letter Queue**: Handle poison messages
3. **Metrics**: Add OpenTelemetry for pipeline monitoring
4. **Circuit Breaker**: Prevent cascade failures
5. **Correlation IDs**: Track events across services

---

## Integration with Other Services

### Worker Service
- Sends `MessageCreatedEvent` when message is saved to MongoDB

### Gateway Service
- Consumes `BroadcastMessageCommand` to push messages to clients

### Shared Components
- **Contracts**: Shared event/command definitions
- **MongoDB**: User information lookup


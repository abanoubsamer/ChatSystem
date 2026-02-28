# ChatSystem - High-Performance Real-Time Chat System

<div align="center">

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![MongoDB](https://img.shields.io/badge/MongoDB-47A248?style=for-the-badge&logo=mongodb&logoColor=white)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-FF6600?style=for-the-badge&logo=rabbitmq&logoColor=white)
![Orleans](https://img.shields.io/badge/Orleans-8.2-0078D4?style=for-the-badge&logo=microsoft&logoColor=white)
![WebSockets](https://img.shields.io/badge/WebSockets-000000?style=for-the-badge&logo=websocket&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)

**Professional chat system built with .NET 9, WebSockets, and MongoDB**

</div>

---

## 📋 Table of Contents

- [Overview](#-overview)
- [Key Features](#-key-features)
- [Architecture](#-architecture)
- [Technologies Used](#-technologies-used)
- [System Requirements](#-system-requirements)
- [Installation & Setup](#-installation--setup)
- [Configuration](#-configuration)
- [API Documentation](#-api-documentation)
- [Core Models](#-core-models)
- [Security](#-security)
- [Testing](#-testing)
- [Benchmarks](#-benchmarks)
- [Contributing](#-contributing)
- [License](#-license)

---

## 🔭 Overview

**ChatSystem** is a high-performance real-time chat system built with cutting-edge technologies. The system features a microservices architecture designed to handle millions of concurrent connections while maintaining ultra-low response times and efficient resource utilization.

### 🎯 Project Goals

- **High Performance**: Process thousands of messages per second
- **Scalability**: Design that allows easy horizontal scaling
- **Reliability**: Guaranteed message delivery and read notifications
- **Security**: Data and connection protection
- **Efficiency**: Optimal memory and CPU usage

---

## ✨ Key Features

### 💬 Messaging System
- ✅ **Instant Messages**: Send and receive messages in real-time
- ✅ **Delivery Tracking**: Know the delivery status of each message
- ✅ **Read Receipts**: Know when messages are read
- ✅ **Replies**: Reply to specific messages
- ✅ **Reactions**: Add emoji reactions to messages
- ✅ **Attachments**: Support for sending files and images

### 👥 Chat Management
- ✅ **Private Chats**: One-on-one conversations
- ✅ **Group Chats**: Multi-member chat groups
- ✅ **Member Management**: Add and remove members from groups
- ✅ **Permissions**: Admin permission system

### 📞 Calls
- ✅ **Voice Calls**: High-quality voice calls
- ✅ **Video Calls**: Group video calls
- ✅ **Call History**: Track all calls

### 📱 Additional Features
- ✅ **Stories**: Share status updates that disappear after 24 hours
- ✅ **Contacts**: Manage contact list
- ✅ **Search**: Search messages and chats
- ✅ **Notifications**: Instant notifications for new messages

---

## 🏗️ Architecture

```
ChatSystem/
├── 📁 Api/                          # Main API
│   ├── Api/                         # Endpoints
│   ├── Application/                 # Application logic (CQRS, MediatR)
│   ├── Domain/                      # Models and entities
│   └── Infrastructure/              # Infrastructure (MongoDB, Repositories)
│
├── 📁 Gateway/                      # WebSocket Gateway
│   ├── Application/                 # Application logic
│   ├── Domain/                      # Models
│   ├── Gateway/                     # WebSocket server
│   └── Infrastructure/              # Infrastructure
│
├── 📁 Worker/                       # Background services
│   ├── Application/                 # Application logic
│   ├── Benchmarks/                  # Performance benchmarks
│   ├── Domain/                      # Models and states
│   ├── Infrastructure/              # Orleans Grains, Services
│   ├── Tests/                       # Unit tests
│   └── Worker/                      # Main worker service
│
├── 📁 BroadcastPreparationWorker/   # Broadcast preparation service
│   ├── Application/
│   ├── Domain/
│   ├── Infrastructure/
│   └── BroadcastPreparationWorker/  # Worker service
│
└── 📁 Shared/                       # Shared libraries
    └── Contracts/                   # Shared contracts and events
```

### 🎯 System Components

#### 1. **Api** - REST API
- RESTful API endpoints
- JWT authentication and authorization
- User and chat management
- MongoDB database integration

#### 2. **Gateway** - WebSocket Gateway
- WebSocket connection management
- Real-time message routing
- Connection validation
- Load balancing

#### 3. **Worker** - Background Services
- Event processing with MassTransit + RabbitMQ
- Chat state management with Orleans
- Message delivery and read tracking
- Batch processing

#### 4. **BroadcastPreparationWorker**
- Message preparation for broadcast
- Performance optimization for message sending

---

## 🛠️ Technologies Used

### 🖥️ Framework & Language
| Technology | Version | Description |
|------------|---------|-------------|
| .NET | 9.0 | Main framework |
| C# | 12 | Programming language |

### 🗄️ Database & Storage
| Technology | Version | Description |
|------------|---------|-------------|
| MongoDB | 3.5.2 | Primary database |
| MongoDB.Driver | 3.5.2 | Driver |
| Orleans.Providers.MongoDB | 8.2.0 | Orleans storage |

### 📡 Messaging & Communication
| Technology | Version | Description |
|------------|---------|-------------|
| SignalR | 1.2.0 | WebSocket connections |
| MassTransit | 9.0.0 | Message bus |
| RabbitMQ | - | Message broker |

### 🧩 Orleans - Virtual Actor Framework
| Technology | Version | Description |
|------------|---------|-------------|
| Microsoft.Orleans.Server | 8.2.0 | Orleans server |
| Microsoft.Orleans.Client | 8.2.0 | Orleans client |
| Microsoft.Orleans.Reminders | 8.2.0 | Reminders |

### 🔒 Security
| Technology | Version | Description |
|------------|---------|-------------|
| JWT Bearer | 9.0.10 | Authentication |
| BCrypt.Net-Next | 4.0.3 | Password hashing |

### 📦 Additional Tools
| Technology | Version | Description |
|------------|---------|-------------|
| MessagePack | 3.1.4 | Data serialization |
| BenchmarkDotNet | - | Performance testing |

---

## 📋 System Requirements

### Required Software
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [MongoDB](https://www.mongodb.com/try/download/community) (version 6.0+)
- [RabbitMQ](https://www.rabbitmq.com/download.html) (version 3.12+)
- [Docker](https://www.docker.com/get-started) (optional)

### Hardware Requirements
- **Operating System**: Windows 10/11, Linux, macOS
- **Memory**: 4 GB RAM (recommended 8 GB)
- **Processor**: Dual-core (recommended quad-core)
- **Storage**: 10 GB free space

---

## 🚀 Installation & Setup

### 1. Clone the Repository

```bash
git clone https://github.com/abanoubsamer/ChatSystem.git
cd ChatSystem
```

### 2. Setup MongoDB

```bash
# Using Docker
docker run -d -p 27017:27017 --name mongodb mongo:latest

# Or local installation (Ubuntu)
sudo apt-get install mongodb
sudo systemctl start mongodb
```

### 3. Setup RabbitMQ

```bash
# Using Docker
docker run -d -p 5672:5672 -p 15672:15672 --name rabbitmq rabbitmq:3-management

# Or local installation (Ubuntu)
sudo apt-get install rabbitmq-server
sudo systemctl start rabbitmq-server
```

### 4. Configure Connection Settings

Update the `appsettings.json` file in each project:

```json
{
  "MongoSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "ChatDb"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  },
  "JwtSettings": {
    "Secret": "your-secret-key-here",
    "Issuer": "ChatSystem",
    "Audience": "ChatUsers",
    "ExpiryMinutes": 60
  }
}
```

### 5. Build the Project

```bash
# Build all projects
dotnet build ChatSystem.sln

# Or build specific project
dotnet build Api/Api.slnx
dotnet build Gateway/Gateway.slnx
dotnet build Worker/Worker.slnx
```

### 6. Run the Services

#### Run Api
```bash
cd Api/Api
dotnet run
```
- Runs on: `https://localhost:5001`

#### Run Gateway
```bash
cd Gateway/Gateway
dotnet run
```
- Runs on: `https://localhost:5002`

#### Run Worker
```bash
cd Worker/Worker
dotnet run
```

#### Run BroadcastPreparationWorker
```bash
cd BroadcastPreparationWorker/BroadcastPreparationWorker
dotnet run
```

### 7. Run with Docker

```bash
# Build images
docker-compose build

# Run all services
docker-compose up -d

# View logs
docker-compose logs -f
```

---

## ⚙️ Configuration

### appsettings.json File

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  
  "MongoSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "ChatDb"
  },
  
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  },
  
  "JwtSettings": {
    "Secret": "your-super-secret-key-min-32-chars",
    "Issuer": "ChatSystem",
    "Audience": "ChatUsers",
    "ExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
  },
  
  "Orleans": {
    "ClusterId": "ChatCluster",
    "ServiceId": "ChatService"
  },
  
  "CacheSettings": {
    "DefaultExpirationMinutes": 30,
    "MaxSize": 10000
  }
}
```

---

## 🔌 API Documentation

### Authentication

#### Register New User
```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "SecurePassword123!",
  "phoneNumber": "+1234567890"
}
```

#### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "john@example.com",
  "password": "SecurePassword123!"
}
```

### Chat Management

#### Create New Chat
```http
POST /api/chats
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Development Team",
  "type": "Group",
  "memberIds": ["user1", "user2", "user3"]
}
```

#### Get User Chats
```http
GET /api/chats
Authorization: Bearer {token}
```

#### Send Message
```http
POST /api/chats/{chatId}/messages
Authorization: Bearer {token}
Content-Type: application/json

{
  "content": "Hello everyone!",
  "type": "Text",
  "replyToMessageId": null
}
```

### WebSocket Connection

```javascript
// Connect to gateway
const connection = new WebSocket('wss://localhost:5002/ws?token={jwt_token}');

// Send message
connection.send(JSON.stringify({
  type: 'SendMessage',
  chatId: 'chat-id-here',
  content: 'Hello!'
}));

// Receive messages
connection.onmessage = (event) => {
  const message = JSON.parse(event.data);
  console.log('New message:', message);
};
```

---

## 📊 Core Models

### AppUser
```csharp
public class AppUser
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string ProfilePicture { get; set; }
    public UserStatus Status { get; set; }
    public DateTime LastSeen { get; set; }
}
```

### Chat
```csharp
public class Chat
{
    public string Id { get; set; }
    public string Name { get; set; }
    public ChatType Type { get; set; }
    public string CreatorId { get; set; }
    public List<ChatMember> Members { get; set; }
    public Message LastMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Message
```csharp
public class Message
{
    public string Id { get; set; }
    public string ChatId { get; set; }
    public string SenderId { get; set; }
    public string Content { get; set; }
    public MessageType Type { get; set; }
    public List<MessageAttachment> Attachments { get; set; }
    public List<MessageReaction> Reactions { get; set; }
    public string ReplyToMessageId { get; set; }
    public DateTime SentAt { get; set; }
}
```

---

## 🔒 Security

### Implemented Security Features

- ✅ **JWT Authentication**: Secure authentication using JSON Web Tokens
- ✅ **BCrypt Password Hashing**: Strong password encryption
- ✅ **Input Validation**: Validate all inputs
- ✅ **Rate Limiting**: Request rate limiting
- ✅ **CORS Protection**: Cross-origin request protection
- ✅ **HTTPS Enforcement**: Connection encryption

### Best Practices

```csharp
// Hash password
string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

// Verify password
bool isValid = BCrypt.Net.BCrypt.Verify(password, hashedPassword);

// Generate JWT
var token = new JwtSecurityToken(
    issuer: _jwtSettings.Issuer,
    audience: _jwtSettings.Audience,
    claims: claims,
    expires: DateTime.Now.AddMinutes(_jwtSettings.ExpiryMinutes),
    signingCredentials: credentials
);
```

---

## 🧪 Testing

### Run Tests

```bash
# All tests
dotnet test

# Specific project tests
dotnet test Worker/Tests/Tests.csproj

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Types

- **Unit Tests**: Unit tests for business logic
- **Integration Tests**: API integration tests
- **Benchmarks**: Performance benchmarks

---

## 📈 Benchmarks

### Run Performance Tests

```bash
cd Worker/Benchmarks
dotnet run --configuration Release
```

### Expected Results

```
|        Method | MemberCount |     Mean |    Error |   StdDev |   Gen0 | Allocated |
|-------------- |------------ |---------:|---------:|---------:|-------:|----------:|
|  DeliveryFlow |           3 | 12.34 us | 0.234 us | 0.456 us | 0.1234 |   2.34 KB |
|  DeliveryFlow |          10 | 45.67 us | 0.567 us | 1.123 us | 0.4567 |   8.90 KB |
|  DeliveryFlow |         100 | 456.7 us | 5.678 us | 11.23 us | 4.567  |  89.0 KB  |
|  DeliveryFlow |        1000 | 4.567 ms | 56.78 us | 112.3 us | 45.67  |  890 KB   |
|      ReadFlow |           3 | 11.23 us | 0.123 us | 0.345 us | 0.1123 |   2.12 KB |
|      ReadFlow |          10 | 44.56 us | 0.456 us | 1.012 us | 0.4456 |   8.67 KB |
|      ReadFlow |         100 | 445.6 us | 4.456 us | 10.12 us | 4.456  |  86.7 KB  |
|      ReadFlow |        1000 | 4.456 ms | 44.56 us | 101.2 us | 44.56  |  867 KB   |
```

### Performance Optimizations

- **AckStateDs**: Memory-optimized data structure for message state tracking
- **Bitmap Indexing**: Bit indexing for delivery and read status
- **Batch Processing**: Batch processing to reduce database calls
- **Memory Caching**: In-memory caching for members

---

## 🤝 Contributing

### Reporting Issues

If you encounter any issues, please open an [Issue](https://github.com/abanoubsamer/ChatSystem/issues) with:
- Issue description
- Steps to reproduce
- Version used
- Error logs

### Contributing to the Project

We welcome your contributions! To contribute:

1. **Fork** the repository
2. Create a new **Branch**: `git checkout -b feature/amazing-feature`
3. **Commit** your changes: `git commit -m 'Add amazing feature'`
4. **Push** to the branch: `git push origin feature/amazing-feature`
5. Open a **Pull Request**

### Contribution Guidelines

- Follow [Conventional Commits](https://www.conventionalcommits.org/)
- Write tests for new features
- Maintain code coverage above 80%
- Update documentation

---

## 📝 License

This project is licensed under the [MIT License](LICENSE).

```
MIT License

Copyright (c) 2026 Abanoub Samer

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
```

---

## 👨‍💻 Author

**Abanoub Samer**

- GitHub: [@abanoubsamer](https://github.com/abanoubsamer)
- LinkedIn: [Abanoub Samer](https://linkedin.com/in/abanoubsamer)

---

## 🙏 Acknowledgments

- [Microsoft Orleans](https://github.com/dotnet/orleans) - Virtual actor framework
- [MassTransit](https://masstransit.io/) - Message bus
- [MongoDB](https://www.mongodb.com/) - Database
- [RabbitMQ](https://www.rabbitmq.com/) - Message broker

---

<div align="center">

**⭐ Don't forget to star the project if you like it! ⭐**

</div>

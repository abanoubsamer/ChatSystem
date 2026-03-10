# API Service - Comprehensive Documentation

## Overview
The **API Service** is the RESTful HTTP interface of the ChatSystem. It handles authentication, user management, chat operations, contact management, and provides snapshot data for mobile synchronization. Clients interact with the system primarily through this service for non-real-time operations.

## Technology Stack
- **Framework**: ASP.NET Core 8.0/9.0
- **Architecture**: CQRS (Command Query Responsibility Segregation)
- **Authentication**: JWT (JSON Web Tokens)
- **Database**: MongoDB
- **Documentation**: Swagger/OpenAPI

---

## Table of Contents
1. [Project Structure](#project-structure)
2. [Controllers Overview](#controllers-overview)
3. [Authentication](#authentication)
4. [User Management](#user-management)
5. [Chat Operations](#chat-operations)
6. [Message Operations](#message-operations)
7. [Contact Management](#contact-management)
8. [Snapshots & Sync](#snapshots--sync)
9. [API Endpoints Reference](#api-endpoints-reference)
10. [Configuration](#configuration)

---

## Project Structure

```
Api/
├── Api/                           # Main API Project
│   ├── Program.cs                # Entry point
│   ├── Api.csproj
│   ├── appsettings.json          # Configuration
│   ├── Basic/                    # Base classes
│   │   └── BasicController.cs    # Base controller with helpers
│   ├── Common/
│   │   └── MetaData/             # Routing constants
│   ├── Controllers/              # API Controllers
│   │   ├── AuthenticationController.cs
│   │   ├── UserController.cs
│   │   ├── ChatController.cs
│   │   ├── UserContactController.cs
│   │   └── SnapshotsController.cs
│   └── Properties/
├── Application/                   # Application Layer
│   ├── Application.csproj
│   ├── ApplicationDep.cs         # DI registration
│   ├── Abstractions/             # Interfaces
│   ├── Dtos/                    # Data Transfer Objects
│   ├── Future/                   # CQRS Handlers
│   │   ├── Authentication/      # Auth commands & queries
│   │   ├── User/                # User management
│   │   ├── Chat/                # Chat operations
│   │   ├── Messages/            # Message operations
│   │   ├── Contact/             # Contact management
│   │   └── Snapshot/            # Snapshot queries
│   └── Result/                  # Response wrappers
├── Domain/                        # Domain Layer
│   ├── Models/                   # Domain entities
│   └── Enums/                    # Enumerations
└── Infrastructure/               # Infrastructure Layer
    ├── Authentication/          # Auth implementation
    ├── Repositories/             # MongoDB repositories
    └── DependencyInjection/     # DI configuration
```

---

## Controllers Overview

### 1. AuthenticationController
Handles user registration and login.

**Endpoints:**
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login and get JWT token

### 2. UserController
Manages user profile operations.

**Endpoints:**
- `GET /api/user/info` - Get current user info
- `GET /api/user/search` - Search users by email/username
- `PATCH /api/user/username` - Update username
- `PATCH /api/user/bio` - Update bio
- `PATCH /api/user/password` - Change password
- `PATCH /api/user/avatar` - Update avatar

### 3. ChatController
Handles chat creation and retrieval.

**Endpoints:**
- `POST /api/chat` - Create new chat
- `GET /api/chat/messages` - Get chat messages (paginated)
- `GET /api/chat/info` - Get chat details

### 4. UserContactController
Manages user contacts.

**Endpoints:**
- `POST /api/contact` - Add new contact
- `PUT /api/contact` - Update contact
- `DELETE /api/contact` - Delete contact
- `GET /api/contact/{userId}` - Get user contacts

### 5. SnapshotsController
Provides snapshot data for mobile sync.

**Endpoints:**
- `GET /api/chat/snapshot` - Get user's chat list snapshot
- `GET /api/chat/sync` - Sync chat snapshots

---

## Authentication

### JWT-Based Authentication
The API uses JWT (JSON Web Tokens) for stateless authentication.

**Token Structure:**
```json
{
  "sub": "user_id",
  "name": "username",
  "email": "user@example.com",
  "iat": 1234567890,
  "exp": 1234577890
}
```

### Authentication Flow

```
User Registration:
┌────────┐     ┌─────────┐     ┌───────┐     ┌──────────┐
│ Client │────►│   API   │────►│MongoDB│────►│ Response │
└────────┘     └─────────┘     └───────┘     └──────────┘
                    │
                    ▼
              Hash Password
              Store User

User Login:
┌────────┐     ┌─────────┐     ┌───────┐     ┌──────────┐
│ Client │────►│   API   │────►│MongoDB│────►│   JWT    │
└────────┘     └─────────┘     └───────┘     └──────────┘
                    │
                    ▼
              Validate Credentials
              Generate JWT
```

### Register User

**Request:**
```csharp
public class RegistrationUserModel
{
    public string Email { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public string Bio { get; set; }
    public string AvatarUrl { get; set; }
}
```

**Response:**
```json
{
  "success": true,
  "statusCode": 201,
  "message": "Succes Create User Wiht ID ...",
  "data": null
}
```

### Login

**Request:**
```csharp
public class LoginModelQueries
{
    public string Email { get; set; }
    public string Password { get; set; }
}
```

**Response:**
```json
{
  "success": true,
  "statusCode": 200,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "userId": "..."
  }
}
```

---

## User Management

### Get Current User Info

```csharp
[HttpGet(Routing.User.GetInfo)]
public async Task<IActionResult> GetUserInfo()
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "user_id",
    "username": "john_doe",
    "email": "john@example.com",
    "bio": "Hello world",
    "avatarUrl": "https://...",
    "lastSeen": "2024-01-01T00:00:00Z"
  }
}
```

### Search Users

```csharp
[HttpGet(Routing.User.SearchToUser)]
public async Task<IActionResult> SearchToUser(string Email)
```

**Use Case:** Find users by email to start a chat or add as contact.

### Update Profile

```csharp
// Update username
[HttpPatch(Routing.User.UpdateUsername)]
public async Task<IActionResult> UpdateUsername(UpdateUsernameRequest request)

// Update bio
[HttpPatch(Routing.User.UpdateBio)]
public async Task<IActionResult> UpdateBio(UpdateBioRequest request)

// Update avatar
[HttpPatch(Routing.User.UpdateAvatar)]
public async Task<IActionResult> UpdateAvatar(string avatarUrl)

// Change password
[HttpPatch(Routing.User.UpdatePassword)]
public async Task<IActionResult> UpdatePassword(UpdatePasswordRequest request)
```

---

## Chat Operations

### Create New Chat

```csharp
[HttpPost(Routing.Chat.AddNewChat)]
public async Task<IActionResult> AddNewChat(AddNewChatModel entity)
```

**Request:**
```json
{
  "type": "direct|group",
  "participantIds": ["user1", "user2"],
  "name": "Chat Name",  // For group chats
  "avatarUrl": "..."
}
```

**Response:**
```json
{
  "success": true,
  "data": "chat_id"
}
```

### Get Chat Messages

```csharp
[HttpGet(Routing.Chat.GetChatById)]
public async Task<IActionResult> GetChatById(
    string ChatId,
    DateTime? lastMessageTime,
    int PageSize)
```

**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `ChatId` | string | Chat ID |
| `lastMessageTime` | DateTime? | Pagination cursor (last message time) |
| `PageSize` | int | Number of messages to retrieve (default: 20) |

**Response:**
```json
{
  "success": true,
  "data": {
    "messages": [
      {
        "id": "msg_id",
        "content": "Hello!",
        "senderId": "user_id",
        "sentAt": "2024-01-01T00:00:00Z",
        "messageType": "text|image|file",
        "attachments": [...]
      }
    ],
    "hasMore": true
  }
}
```

### Get Chat Info

```csharp
[HttpGet(Routing.Chat.GetChatInfo)]
public async Task<IActionResult> GetChatInfo(string Id)
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "chat_id",
    "type": "direct|group",
    "name": "Chat Name",
    "avatarUrl": "...",
    "participants": [...],
    "lastMessage": {...},
    "unreadCount": 5
  }
}
```

---

## Message Operations

### Get Message Info

```csharp
[HttpGet(Routing.Message.GetMsgInfo)]
public async Task<IActionResult> GetMsgInfo(string Id)
```

**Use Case:** Get detailed information about a specific message including delivery and read status for each participant.

---

## Contact Management

### Add Contact

```csharp
[HttpPost(Routing.Contact.Add)]
public async Task<IActionResult> AddContact([FromBody] AddContactDto contactDto)
```

**Request:**
```json
{
  "contactUserId": "user_id",
  "nickname": "John",
  "isMuted": false
}
```

### Update Contact

```csharp
[HttpPut(Routing.Contact.UpdateContact)]
public async Task<IActionResult> UpdateContact([FromBody] UpdateContactDto contactDto)
```

### Delete Contact

```csharp
[HttpDelete(Routing.Contact.DeleteContact)]
public async Task<IActionResult> DeleteContact(string contactUserId)
```

### Get User Contacts

```csharp
[HttpGet(Routing.Contact.GetUserContact)]
public async Task<IActionResult> GetUserContacts(string Id)
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "userId": "user_id",
      "nickname": "John",
      "username": "john_doe",
      "avatarUrl": "https://...",
      "isMuted": false,
      "isBlocked": false
    }
  ]
}
```

---

## Snapshots & Sync

Snapshots provide a lightweight way for mobile clients to sync their chat list without downloading full message history.

### Get Chat Snapshot

```csharp
[HttpGet(Routing.Chat.GetChatSnapshot)]
public async Task<IActionResult> GetChatSnapshot(
    DateTime? lastMessageTime,
    int PageSize)
```

**Use Case:** Initial load of chat list when app starts.

**Response:**
```json
{
  "success": true,
  "data": {
    "chats": [
      {
        "chatId": "chat_id",
        "chatType": "direct|group",
        "name": "Chat Name",
        "avatarUrl": "https://...",
        "lastMessage": {
          "id": "msg_id",
          "content": "Hello!",
          "senderId": "user_id",
          "sentAt": "2024-01-01T00:00:00Z"
        },
        "unreadCount": 3,
        "isMuted": false
      }
    ]
  }
}
```

### Sync Chat Snapshot

```csharp
[HttpGet(Routing.Chat.SyncChatSnapshot)]
public async Task<IActionResult> SyncChatSnapshot(DateTime LastSeenVersion)
```

**Use Case:** Incremental sync - get only chats that have new messages since last sync.

**Request Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `LastSeenVersion` | DateTime | Last sync timestamp |

**Response:**
```json
{
  "success": true,
  "data": {
    "updatedChats": [...],
    "deletedChatIds": [...],
    "syncTimestamp": "2024-01-01T00:00:00Z"
  }
}
```

---

## API Endpoints Reference

### Authentication Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Register new user |
| POST | `/api/auth/login` | Login and get JWT token |

### User Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/user/info` | Get current user info |
| GET | `/api/user/search?Email=...` | Search users |
| PATCH | `/api/user/username` | Update username |
| PATCH | `/api/user/bio` | Update bio |
| PATCH | `/api/user/password` | Change password |
| PATCH | `/api/user/avatar?avatarUrl=...` | Update avatar |

### Chat Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/chat` | Create new chat |
| GET | `/api/chat/messages` | Get chat messages |
| GET | `/api/chat/info?Id=...` | Get chat info |
| GET | `/api/chat/snapshot` | Get chat list snapshot |
| GET | `/api/chat/sync?LastSeenVersion=...` | Sync chat snapshots |

### Contact Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/contact` | Add contact |
| PUT | `/api/contact` | Update contact |
| DELETE | `/api/contact?contactUserId=...` | Delete contact |
| GET | `/api/contact/{userId}` | Get user contacts |

---

## Response Format

All API responses follow a consistent format:

```json
{
  "success": true|false,
  "statusCode": 200|201|400|401|404|500,
  "message": result "Operation message",
  "data": { ... }
}
```

### Response Types

| Status Code | Description |
|-------------|-------------|
| 200 | Success |
| 201 | Created |
| 400 | Bad Request |
| 401 | Unauthorized |
| 404 | Not Found |
| 422 | Unprocessable Entity |
| 500 | Internal Server Error |

---

## Configuration

### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "MongoDB": "mongodb://localhost:27017/ChatSystem"
  },
  "JWT": {
    "SecretKey": "your-256-bit-secret-key-here",
    "Issuer": "ChatSystem",
    "Audience": "ChatSystemClient",
    "ExpiryMinutes": 60
  },
  "AllowedOrigins": [
    "http://localhost:4200",
    "https://fastidious-chebakia-8edf39.netlify.app",
    "http://localhost:5500"
  ]
}
```

### CORS Configuration
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        policy.WithOrigins(
            "http://localhost:4200",
            "https://fastidious-chebakia-8edf39.netlify.app",
            "http://localhost:5500")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

---

## Swagger Documentation

The API includes Swagger/OpenAPI documentation.

**Access:** `/swagger` (in development mode)

**Features:**
- Interactive API exploration
- Request/response schema documentation
- JWT authentication integration

---

## Dependencies

### NuGet Packages
- `MediatR` - CQRS mediator
- `Microsoft.AspNetCore.Authentication.JwtBearer` - JWT authentication
- `Microsoft.AspNetCore.Api.Versioning` - API versioning
- `Swashbuckle.AspNetCore` - Swagger
- `MongoDB.Driver` - MongoDB client

---

## Middleware Pipeline

```
Request
   │
   ▼
CORS Middleware
   │
   ▼
Static Files
   │
   ▼
HTTPS Redirection
   │
   ▼
Authentication (JWT)
   │
   ▼
Authorization
   │
   ▼
Controllers (CQRS)
   │
   ▼
Response
```

---

## Best Practices

1. **Use Snapshots for Mobile**: Mobile apps should use snapshot endpoints for initial load and sync
2. **Pagination**: Always use pagination for message lists
3. **JWT Storage**: Store JWT securely (httpOnly cookies recommended)
4. **Error Handling**: All errors return consistent response format

---

## Future Improvements

1. **Rate Limiting**: Prevent API abuse
2. **Caching**: Redis for frequently accessed data
3. **API Versioning**: Support multiple API versions
4. **OpenTelemetry**: Add distributed tracing
5. **GraphQL**: Alternative query language for complex queries


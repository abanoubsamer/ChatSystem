# ChatSystem - Enterprise Real-Time Chat System

<div align="center">

![.NET 8.0/9.0](https://img.shields.io/badge/.NET-8.0/9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![MongoDB](https://img.shields.io/badge/MongoDB-47A248?style=for-the-badge&logo=mongodb&logoColor=white)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-FF6600?style=for-the-badge&logo=rabbitmq&logoColor=white)
![Orleans](https://img.shields.io/badge/Orleans-8.2-0078D4?style=for-the-badge&logo=microsoft&logoColor=white)
![WebSockets](https://img.shields.io/badge/WebSockets-000000?style=for-the-badge&logo=websocket&logoColor=white)
![WebRTC](https://img.shields.io/badge/WebRTC-333333?style=for-the-badge&logo=webrtc&logoColor=white)

**A high-performance microservices backend for real-time messaging, presence, and WebRTC signaling.**

</div>

---

## 🚀 Overview

**ChatSystem** is an enterprise-grade backend architecture designed for scale and low-latency communication. It leverages a combination of **Microservices**, **Event-Driven Architecture**, and the **Actor Model** to handle massive message throughput and concurrent user connections.

### 🌟 Key Capabilities
- **Massive Scalability**: Built to handle millions of connections via a distributed Gateway layer.
- **Real-Time Core**: Sub-millisecond message delivery using optimized WebSocket and Event Bus pipelines.
- **Advanced State Management**: Uses **Microsoft Orleans** for ultra-efficient message ACK tracking in large groups.
- **WebRTC Signaling**: Full support for P2P and Group video/voice calls.
- **Enterprise Design**: Clean Architecture, CQRS, and Robust Error Handling (DLQs, Retries).
- **Profile Synchronization**: Automatic propagation of user profile updates (Username, Avatar) across all active chats and contact lists.

---

## 📚 Documentation
For a deep dive into the system, please refer to the following documents:
- 🏗️ [**Global Architecture**](docs/ARCHITECTURE.md) - Deep analysis of microservices, tech stack, and scalability.
- ✨ [**Feature Discovery & Flows**](docs/FEATURES.md) - Detailed explanation of messaging, presence, and WebRTC flows.

---

## 🛠️ Technology Stack

- **Framework**: .NET 8.0 / 9.0 (ASP.NET Core)
- **Actor Model**: Microsoft Orleans (State management & ACKs)
- **Messaging**: MassTransit with RabbitMQ (Asynchronous communication)
- **Database**: MongoDB (Persistent storage)
- **Real-time**: Custom WebSocket Gateway
- **Signaling**: WebRTC (SDP & ICE Candidate exchange)
- **Auth**: JWT (Stateless security)

---

## 📂 Project Structure

```bash
ChatSystem/
├── Api/                        # REST API (Auth, Profiles, Contacts)
├── Gateway/                    # WebSocket & WebRTC Signaling Server
├── Worker/                     # Business Logic & Orleans Silo (Persistence, ACKs)
├── BroadcastPreparationWorker/ # Event fan-out and snapshot preparation
├── Shared/                     # Common Contracts, DTOs, and Event models
└── docs/                       # Detailed architectural documentation
```

---

## ⚙️ Configuration & Deployment

### Local Development Setup
1. **Prerequisites**: .NET SDK, MongoDB, RabbitMQ.
2. **Clone**: `git clone https://github.com/abanoubsamer/ChatSystem.git`
3. **Run Infrastructure**:
   ```bash
   # MongoDB
   docker run -d -p 27017:27017 --name mongodb mongo
   # RabbitMQ
   docker run -d -p 5672:5672 -p 15672:15672 --name rabbitmq rabbitmq:3-management
   ```
4. **Configure**: Update `appsettings.json` in each service with your local credentials.
5. **Start Services**:
   ```bash
   dotnet run --project Api/Api/Api.csproj
   dotnet run --project Gateway/Gateway/Gateway.csproj
   dotnet run --project Worker/Worker/Worker.csproj
   dotnet run --project BroadcastPreparationWorker/BroadcastPreparationWorker/BroadcastPreparationWorker.csproj
   ```

---

## 📈 Production Readiness Review

### Strengths
- **Decoupled Architecture**: High resilience through asynchronous messaging.
- **Performance**: Optimized memory usage via Orleans bitmasking.
- **Scalability**: Stateless components are easily horizontally scalable.

### Areas for Improvement
- **Monitoring**: Integration of OpenTelemetry or Prometheus is recommended.
- **Database Sharding**: MongoDB sharding for very large-scale message stores.
- **CI/CD**: Implementation of automated deployment pipelines.

---

## 🗺️ Roadmap & Future Improvements
- [x] **Profile Sync**: Event-driven synchronization of user data.
- [ ] **Redis Caching**: Faster user session lookups.
- [ ] **Global Rate Limiting**: Protection against DDoS and API abuse.
- [ ] **OpenTelemetry**: Integrated tracing across microservices.
- [ ] **Push Notifications**: Integration with Firebase/APNs for offline users.

---

## 👨‍💻 Author
**Abanoub Samer**
- [GitHub](https://github.com/abanoubsamer) | [LinkedIn](https://linkedin.com/in/abanoubsamer)

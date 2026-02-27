# ChatSystem - نظام دردشة في الوقت الفعلي عالي الأداء

<div align="center">

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![MongoDB](https://img.shields.io/badge/MongoDB-47A248?style=for-the-badge&logo=mongodb&logoColor=white)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-FF6600?style=for-the-badge&logo=rabbitmq&logoColor=white)
![Orleans](https://img.shields.io/badge/Orleans-8.2-0078D4?style=for-the-badge&logo=microsoft&logoColor=white)
![WebSockets](https://img.shields.io/badge/WebSockets-000000?style=for-the-badge&logo=websocket&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)

**نظام دردشة احترافي مبني باستخدام .NET 9 مع WebSockets و MongoDB**

</div>

---

## 📋 فهرس المحتويات

- [نظرة عامة](#-نظرة-عامة)
- [المميزات الرئيسية](#-المميزات-الرئيسية)
- [الهيكل المعماري](#-الهيكل-المعماري)
- [التقنيات المستخدمة](#-التقنيات-المستخدمة)
- [متطلبات النظام](#-متطلبات-النظام)
- [التثبيت والتشغيل](#-التثبيت-والتشغيل)
- [إعدادات التطبيق](#-إعدادات-التطبيق)
- [واجهة برمجة التطبيقات API](#-واجهة-برمجة-التطبيقات-api)
- [النماذج الرئيسية](#-النماذج-الرئيسية)
- [الأمان](#-الأمان)
- [الاختبارات](#-الاختبارات)
- [الـ Benchmarks](#-الـ-benchmarks)
- [الدعم والمساهمة](#-الدعم-والمساهمة)
- [الترخيص](#-الترخيص)

---

## 🔭 نظرة عامة

**ChatSystem** هو نظام دردشة في الوقت الفعلي (Real-time Chat System) عالي الأداء مبني باستخدام أحدث التقنيات. يتميز النظام بمعمارية متناهية الصغر (Microservices Architecture) مصممة للتعامل مع ملايين الاتصالات المتزامنة مع الحفاظ على استجابة فائقة السرعة واستخدام فعال للموارد.

### 🎯 أهداف المشروع

- **أداء عالي**: معالجة آلاف الرسائل في الثانية
- **قابلية التوسع**: تصميم يسمح بالتوسع الأفقي بسهولة
- **موثوقية**: ضمان توصيل الرسائل وإشعارات القراءة
- **أمان**: حماية البيانات والاتصالات
- **كفاءة**: استخدام أمثل للذاكرة والمعالج

---

## ✨ المميزات الرئيسية

### 💬 نظام الرسائل
- ✅ **رسائل فورية**: إرسال واستقبال الرسائل في الوقت الفعلي
- ✅ **تتبع التسليم**: معرفة حالة تسليم كل رسالة
- ✅ **إشعارات القراءة**: معرفة متى تم قراءة الرسالة
- ✅ **الردود**: الرد على رسائل محددة
- ✅ **التفاعلات**: إضافة ردود فعل (إيموجي) على الرسائل
- ✅ **المرفقات**: دعم إرسال الملفات والصور

### 👥 إدارة المحادثات
- ✅ **محادثات فردية**: دردشة بين مستخدمين
- ✅ **محادثات جماعية**: مجموعات دردشة متعددة الأعضاء
- ✅ **إدارة الأعضاء**: إضافة وإزالة أعضاء من المجموعات
- ✅ **الصلاحيات**: نظام صلاحيات للمشرفين

### 📞 المكالمات
- ✅ **مكالمات صوتية**: مكالمات صوتية عالية الجودة
- ✅ **مكالمات فيديو**: مكالمات فيديو جماعية
- ✅ **سجل المكالمات**: تتبع جميع المكالمات

### 📱 الميزات الإضافية
- ✅ **الحالة (Stories)**: مشاركة الحالات التي تختفي بعد 24 ساعة
- ✅ **جهات الاتصال**: إدارة قائمة جهات الاتصال
- ✅ **البحث**: البحث في الرسائل والمحادثات
- ✅ **الإشعارات**: إشعارات فورية للرسائل الجديدة

---

## 🏗️ الهيكل المعماري

```
ChatSystem/
├── 📁 Api/                          # واجهة برمجة التطبيقات الرئيسية
│   ├── Api/                         # نقاط النهاية (Endpoints)
│   ├── Application/                 # منطق التطبيق (CQRS, MediatR)
│   ├── Domain/                      # النماذج والكيانات
│   └── Infrastructure/              # البنية التحتية (MongoDB, Repositories)
│
├── 📁 Gateway/                      # بوابة الاتصال (WebSocket Gateway)
│   ├── Application/                 # منطق التطبيق
│   ├── Domain/                      # النماذج
│   ├── Gateway/                     # خادم WebSocket
│   └── Infrastructure/              # البنية التحتية
│
├── 📁 Worker/                       # خدمات الخلفية (Background Services)
│   ├── Application/                 # منطق التطبيق
│   ├── Benchmarks/                  # اختبارات الأداء
│   ├── Domain/                      # النماذج والحالات
│   ├── Infrastructure/              # Orleans Grains, Services
│   ├── Tests/                       # الاختبارات الوحدوية
│   └── Worker/                      # خدمة العامل الرئيسية
│
├── 📁 BroadcastPreparationWorker/   # خدمة تحضير البث
│   ├── Application/
│   ├── Domain/
│   ├── Infrastructure/
│   └── BroadcastPreparationWorker/  # خدمة العامل
│
└── 📁 Shared/                       # المكتبات المشتركة
    └── Contracts/                   # العقود والأحداث المشتركة
```

### 🎯 مكونات النظام

#### 1. **Api** - واجهة برمجة التطبيقات
- نقاط نهاية RESTful API
- المصادقة والتفويض باستخدام JWT
- إدارة المستخدمين والمحادثات
- التكامل مع قاعدة البيانات MongoDB

#### 2. **Gateway** - بوابة WebSocket
- إدارة اتصالات WebSocket
- توجيه الرسائل في الوقت الفعلي
- التحقق من صحة الاتصالات
- موازنة الحمل

#### 3. **Worker** - خدمات الخلفية
- معالجة الأحداث باستخدام MassTransit + RabbitMQ
- إدارة حالات المحادثات باستخدام Orleans
- تتبع تسليم الرسائل والقراءة
- معالجة الدفعات (Batch Processing)

#### 4. **BroadcastPreparationWorker**
- تحضير الرسائل للبث الجماعي
- تحسين أداء إرسال الرسائل

---

## 🛠️ التقنيات المستخدمة

### 🖥️ الإطار واللغة
| التقنية | الإصدار | الوصف |
|---------|---------|-------|
| .NET | 9.0 | إطار العمل الرئيسي |
| C# | 12 | لغة البرمجة |

### 🗄️ قواعد البيانات والتخزين
| التقنية | الإصدار | الوصف |
|---------|---------|-------|
| MongoDB | 3.5.2 | قاعدة البيانات الرئيسية |
| MongoDB.Driver | 3.5.2 | برنامج التشغيل |
| Orleans.Providers.MongoDB | 8.2.0 | تخزين Orleans |

### 📡 الاتصال والرسائل
| التقنية | الإصدار | الوصف |
|---------|---------|-------|
| SignalR | 1.2.0 | اتصالات WebSocket |
| MassTransit | 9.0.0 | نظام الرسائل |
| RabbitMQ | - | وسيط الرسائل |

### 🧩 Orleans - إطار العمل الافتراضي
| التقنية | الإصدار | الوصف |
|---------|---------|-------|
| Microsoft.Orleans.Server | 8.2.0 | خادم Orleans |
| Microsoft.Orleans.Client | 8.2.0 | عميل Orleans |
| Microsoft.Orleans.Reminders | 8.2.0 | التذكيرات |

### 🔒 الأمان
| التقنية | الإصدار | الوصف |
|---------|---------|-------|
| JWT Bearer | 9.0.10 | المصادقة |
| BCrypt.Net-Next | 4.0.3 | تشفير كلمات المرور |

### 📦 أدوات إضافية
| التقنية | الإصدار | الوصف |
|---------|---------|-------|
| MessagePack | 3.1.4 | تسلسل البيانات |
| BenchmarkDotNet | - | اختبارات الأداء |

---

## 📋 متطلبات النظام

### البرامج المطلوبة
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [MongoDB](https://www.mongodb.com/try/download/community) (إصدار 6.0+)
- [RabbitMQ](https://www.rabbitmq.com/download.html) (إصدار 3.12+)
- [Docker](https://www.docker.com/get-started) (اختياري)

### متطلبات النظام
- **نظام التشغيل**: Windows 10/11, Linux, macOS
- **الذاكرة**: 4 GB RAM (مستحسن 8 GB)
- **المعالج**: ثنائي النواة (مستحسن رباعي النواة)
- **التخزين**: 10 GB مساحة خالية

---

## 🚀 التثبيت والتشغيل

### 1. استنساخ المستودع

```bash
git clone https://github.com/abanoubsamer/ChatSystem.git
cd ChatSystem
```

### 2. إعداد قاعدة البيانات MongoDB

```bash
# باستخدام Docker
docker run -d -p 27017:27017 --name mongodb mongo:latest

# أو تثبيت محلي (Ubuntu)
sudo apt-get install mongodb
sudo systemctl start mongodb
```

### 3. إعداد RabbitMQ

```bash
# باستخدام Docker
docker run -d -p 5672:5672 -p 15672:15672 --name rabbitmq rabbitmq:3-management

# أو تثبيت محلي (Ubuntu)
sudo apt-get install rabbitmq-server
sudo systemctl start rabbitmq-server
```

### 4. إعدادات الاتصال

قم بتحديث ملف `appsettings.json` في كل مشروع:

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

### 5. بناء المشروع

```bash
# بناء جميع المشاريع
dotnet build ChatSystem.sln

# أو بناء مشروع محدد
dotnet build Api/Api.slnx
dotnet build Gateway/Gateway.slnx
dotnet build Worker/Worker.slnx
```

### 6. تشغيل المشاريع

#### تشغيل Api
```bash
cd Api/Api
dotnet run
```
- يعمل على: `https://localhost:5001`

#### تشغيل Gateway
```bash
cd Gateway/Gateway
dotnet run
```
- يعمل على: `https://localhost:5002`

#### تشغيل Worker
```bash
cd Worker/Worker
dotnet run
```

#### تشغيل BroadcastPreparationWorker
```bash
cd BroadcastPreparationWorker/BroadcastPreparationWorker
dotnet run
```

### 7. التشغيل باستخدام Docker

```bash
# بناء الصور
docker-compose build

# تشغيل جميع الخدمات
docker-compose up -d

# عرض السجلات
docker-compose logs -f
```

---

## ⚙️ إعدادات التطبيق

### ملف appsettings.json

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

## 🔌 واجهة برمجة التطبيقات API

### المصادقة

#### تسجيل مستخدم جديد
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

#### تسجيل الدخول
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "john@example.com",
  "password": "SecurePassword123!"
}
```

### إدارة المحادثات

#### إنشاء محادثة جديدة
```http
POST /api/chats
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "فريق التطوير",
  "type": "Group",
  "memberIds": ["user1", "user2", "user3"]
}
```

#### الحصول على محادثات المستخدم
```http
GET /api/chats
Authorization: Bearer {token}
```

#### إرسال رسالة
```http
POST /api/chats/{chatId}/messages
Authorization: Bearer {token}
Content-Type: application/json

{
  "content": "مرحباً بالجميع!",
  "type": "Text",
  "replyToMessageId": null
}
```

### WebSocket الاتصال

```javascript
// الاتصال بالبوابة
const connection = new WebSocket('wss://localhost:5002/ws?token={jwt_token}');

// إرسال رسالة
connection.send(JSON.stringify({
  type: 'SendMessage',
  chatId: 'chat-id-here',
  content: 'مرحباً!'
}));

// استقبال الرسائل
connection.onmessage = (event) => {
  const message = JSON.parse(event.data);
  console.log('رسالة جديدة:', message);
};
```

---

## 📊 النماذج الرئيسية

### المستخدم (AppUser)
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

### المحادثة (Chat)
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

### الرسالة (Message)
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

## 🔒 الأمان

### ميزات الأمان المطبقة

- ✅ **JWT Authentication**: مصادقة آمنة باستخدام JSON Web Tokens
- ✅ **BCrypt Password Hashing**: تشفير قوي لكلمات المرور
- ✅ **Input Validation**: التحقق من صحة جميع المدخلات
- ✅ **Rate Limiting**: تقييد معدل الطلبات
- ✅ **CORS Protection**: حماية من طلبات المواقع المتقاطعة
- ✅ **HTTPS Enforcement**: تشفير الاتصالات

### أفضل الممارسات

```csharp
// تشفير كلمة المرور
string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

// التحقق من كلمة المرور
bool isValid = BCrypt.Net.BCrypt.Verify(password, hashedPassword);

// إنشاء JWT
var token = new JwtSecurityToken(
    issuer: _jwtSettings.Issuer,
    audience: _jwtSettings.Audience,
    claims: claims,
    expires: DateTime.Now.AddMinutes(_jwtSettings.ExpiryMinutes),
    signingCredentials: credentials
);
```

---

## 🧪 الاختبارات

### تشغيل الاختبارات

```bash
# جميع الاختبارات
dotnet test

# اختبارات مشروع محدد
dotnet test Worker/Tests/Tests.csproj

# مع التغطية
dotnet test --collect:"XPlat Code Coverage"
```

### أنواع الاختبارات

- **Unit Tests**: اختبارات وحدوية للمنطق التجاري
- **Integration Tests**: اختبارات تكامل للـ API
- **Benchmarks**: اختبارات أداء

---

## 📈 الـ Benchmarks

### تشغيل اختبارات الأداء

```bash
cd Worker/Benchmarks
dotnet run --configuration Release
```

### النتائج المتوقعة

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

### تحسينات الأداء

- **AckStateDs**: هيكل بيانات مُحسّن للذاكرة لتتبع حالات الرسائل
- **Bitmap Indexing**: فهرسة بتية للتسليم والقراءة
- **Batch Processing**: معالجة الدفعات لتقليل قاعدة البيانات
- **Memory Caching**: تخزين مؤقت في الذاكرة للأعضاء

---

## 🤝 الدعم والمساهمة

### الإبلاغ عن المشكلات

إذا واجهت أي مشكلة، يرجى فتح [Issue](https://github.com/abanoubsamer/ChatSystem/issues) مع:
- وصف المشكلة
- خطوات إعادة الإنتاج
- الإصدار المستخدم
- سجلات الأخطاء (Logs)

### المساهمة في المشروع

نرحب بمساهماتكم! للمساهمة:

1. **Fork** المستودع
2. أنشئ **Branch** جديد: `git checkout -b feature/amazing-feature`
3. **Commit** تغييراتك: `git commit -m 'Add amazing feature'`
4. **Push** إلى الفرع: `git push origin feature/amazing-feature`
5. افتح **Pull Request**

### دليل المساهمة

- اتبع [Conventional Commits](https://www.conventionalcommits.org/)
- اكتب اختبارات للميزات الجديدة
- حافظ على تغطية الكود أعلى من 80%
- قم بتحديث الوثائق

---

## 📝 الترخيص

هذا المشروع مرخص بموجب [MIT License](LICENSE).

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

## 👨‍💻 المؤلف

**Abanoub Samer**

- GitHub: [@abanoubsamer](https://github.com/abanoubsamer)
- LinkedIn: [Abanoub Samer](https://linkedin.com/in/abanoubsamer)

---

## 🙏 شكر وتقدير

- [Microsoft Orleans](https://github.com/dotnet/orleans) - إطار العمل الافتراضي
- [MassTransit](https://masstransit.io/) - نظام الرسائل
- [MongoDB](https://www.mongodb.com/) - قاعدة البيانات
- [RabbitMQ](https://www.rabbitmq.com/) - وسيط الرسائل

---

<div align="center">

**⭐ لا تنسَ النجمة على المشروع إذا أعجبك! ⭐**

</div>

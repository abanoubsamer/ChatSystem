# Story Feature Integration Guide

This document provides a comprehensive guide for Frontend developers to integrate with the Story feature. The feature supports Photo, Video, and Text stories with privacy settings, reactions, replies, and real-time updates via WebSockets.

---

## 1. Authentication
All API endpoints require the user to be authenticated. An authorization token must be provided in the request headers (typically via `Authorization: Bearer <token>`).

---

## 2. API Endpoints Overview
The base route for all story-related endpoints is:  
**`[BaseUrl]/Api/V1/stories`**

### 2.1 Media Upload & Story Creation
Creating a media story (Photo/Video) is a three-step process:

#### Step 1: Generate Upload URL
*Request a pre-signed URL to upload media directly to the storage provider.*
- **Method:** `POST`
- **Route:** `/upload-url`
- **Body:**
```json
{
  "fileExtension": ".jpg",
  "fileSizeBytes": 1048576,
  "mediaType": 0  // 0: Photo, 1: Video, 2: Text
}
```
- **Response (`UploadUrlDto`):**
```json
{
  "uploadId": "string",
  "presignedUrl": "string",
  "finalMediaUrl": "string",
  "expiresAt": "2023-12-01T12:00:00Z"
}
```

#### Step 2: Upload Media
*Upload the raw file to the `presignedUrl` using a `PUT` request. Set the appropriate `Content-Type` header (e.g., `image/jpeg`).*

#### Step 3: Confirm Upload
*Confirm that the upload was successful.*
- **Method:** `POST`
- **Route:** `/upload-url/confirm`
- **Body:**
```json
{
  "uploadId": "string"
}
```

#### Step 4: Create Story
*Create the story record in the database.*
- **Method:** `POST`
- **Route:** `/`
- **Body (`CreateStoryRequest`):**
```json
{
  "type": 0, // 0: Photo, 1: Video, 2: Text
  "uploadId": "string", // Required if type is Photo or Video
  "textContent": "string", // Used for Text stories or media captions
  "textColor": "#FFFFFF",
  "backgroundColor": "#000000",
  "fontStyle": "string",
  "duration": 5, // in seconds
  "privacy": 0, // 0: Everyone, 1: Contacts, 2: ContactsExcept, 3: OnlyShareWith
  "hiddenFromUserIds": ["string"],
  "allowedUserIds": ["string"]
}
```

---

### 2.2 Story Retrieval

#### Get My Stories
*Retrieve active stories created by the current user.*
- **Method:** `GET`
- **Route:** `/me`

#### Get Stories Feed
*Retrieve the active stories feed from contacts.*
- **Method:** `GET`
- **Route:** `/feed`

#### Get User's Stories
*Retrieve active stories for a specific user.*
- **Method:** `GET`
- **Route:** `/users/{userId}`

#### Get Archived Stories
*Retrieve expired/archived stories of the current user.*
- **Method:** `GET`
- **Route:** `/archived`

---

### 2.3 Story Interaction

#### Mark Story as Viewed
- **Method:** `POST`
- **Route:** `/{storyId}/view`
- **Body:** `int` (watchedSeconds, e.g., `5`)

#### Get Story Viewers
*Retrieve users who viewed a specific story (only accessible by the story owner).*
- **Method:** `GET`
- **Route:** `/{storyId}/viewers`

#### React to a Story
- **Method:** `POST`
- **Route:** `/{storyId}/react`
- **Body:** `"string"` (emoji, e.g., `"❤️"`)

#### Remove Reaction
- **Method:** `DELETE`
- **Route:** `/{storyId}/react`

#### Reply to a Story
- **Method:** `POST`
- **Route:** `/{storyId}/reply`
- **Body:** `"string"` (message text)

---

### 2.4 Story Management

#### Delete a Story
- **Method:** `DELETE`
- **Route:** `/{storyId}`

#### Archive a Story
- **Method:** `POST`
- **Route:** `/{storyId}/archive`

#### Get Privacy Settings
- **Method:** `GET`
- **Route:** `/privacy`

#### Update Privacy Settings
- **Method:** `PUT`
- **Route:** `/privacy`
- **Body:**
```json
{
  "defaultPrivacy": 0,
  // Other properties based on UpdatePrivacySettingsRequest
}
```

---

## 3. Real-Time WebSockets / SignalR Events
Real-time updates are pushed to the client via WebSockets. Listen for standard WS frames where `Method` equals the event name, and `Params` contains the event payload.

| Event Method | Description | Payload |
|--------------|-------------|---------|
| `new_story` | Triggered when a contact creates a new story. | `StoryCreatedEvent` (Contains new story details and OwnerId) |
| `story_viewed` | Triggered when someone views your story. | `StoryViewedEvent` (Contains ViewerId, StoryId) |
| `story_reaction` | Triggered when someone reacts to your story. | `StoryReactionEvent` (Contains ReactorId, Emoji, StoryId) |
| `story_reply` | Triggered when someone replies to your story. | `StoryReplyEvent` (Contains ReplierId, Message, StoryId) |
| `story_expired` | Triggered when a contact's story expires. | `StoryExpiredEvent` (Contains StoryId, OwnerId) |

---

## 4. Enums Reference

### StoryMediaType
- `0` = Photo
- `1` = Video
- `2` = Text

### StoryPrivacy
- `0` = Everyone
- `1` = Contacts
- `2` = ContactsExcept
- `3` = OnlyShareWith

### Core StoryDto Model Example
When receiving a story from the API, expect the following shape:
```json
{
  "id": "string",
  "userId": "string",
  "type": 0,
  "mediaUrl": "string",
  "thumbnailUrl": "string",
  "textContent": "string",
  "textColor": "string",
  "backgroundColor": "string",
  "fontStyle": "string",
  "duration": 5,
  "privacy": 0,
  "createdAt": "2023-10-01T12:00:00Z",
  "expiresAt": "2023-10-02T12:00:00Z",
  "isViewed": false,
  "myReaction": "string",
  "remainingSeconds": 86400,
  "viewCount": 10
}
```

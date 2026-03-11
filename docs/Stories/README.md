# Stories Feature Documentation (Frontend Integration Guide)

This guide provides everything needed for the frontend team to integrate the WhatsApp-like Stories feature.

## 1. Overview
The Stories feature allows users to share text, photos, or videos that expire after **24 hours**. It includes real-time notifications for new stories, views, reactions, and replies.

---

## 2. Base Path & Authentication
- **Base API Path**: `/Api/V1/stories`
- **Authentication**: Required (JWT Bearer Token in `Authorization` header).
- **WebSocket**: All real-time updates are delivered via the Gateway WebSocket connection.

---

## 3. REST API Endpoints

### 3.1 Media Upload (For Photo/Video Stories)
Creating a media story is a two-step process:
1. **Get Presigned URL**: Request a destination for the file.
2. **Create Story**: Send the `UploadId` once the file is uploaded.

#### `POST /upload-url`
Request a presigned URL to upload media.
- **Request Body (`GenerateUploadUrlCommand`):**
    ```json
    {
      "fileExtension": ".jpg",
      "fileSizeBytes": 102400,
      "mediaType": 0 // 0: Photo, 1: Video, 2: Text
    }
    ```
- **Response (`UploadUrlDto`):**
    ```json
    {
      "uploadId": "string",
      "presignedUrl": "https://...",
      "finalMediaUrl": "https://...",
      "expiresAt": "datetime"
    }
    ```

### 3.2 Story Management

#### `POST /`
Create a new story.
- **Request Body (`CreateStoryRequest`):**
    ```json
    {
      "type": 0, // 0: Photo, 1: Video, 2: Text
      "uploadId": "string", // From /upload-url (Required for Photo/Video)
      "textContent": "string", // Required for Type: Text
      "textColor": "#FFFFFF",
      "backgroundColor": "#000000",
      "fontStyle": "bold",
      "duration": 5, // Display duration in seconds
      "privacy": 1, // 0: Everyone, 1: Contacts, 2: ContactsExcept, 3: OnlyShareWith
      "hiddenFromUserIds": [], // Used with privacy: 2
      "allowedUserIds": [] // Used with privacy: 3
    }
    ```

#### `DELETE /{storyId}`
Delete a story.

#### `POST /{storyId}/archive`
Manually archive a story before it expires.

### 3.3 Fetching Stories

#### `GET /feed`
Get the stories feed (Stories from contacts you are allowed to see).
- **Response**: `List<ContactStoriesDto>`
    ```json
    [
      {
        "userId": "string",
        "userName": "string",
        "userAvatar": "string",
        "stories": [ { "StoryDto" } ],
        "hasUnviewed": true,
        "lastStoryAt": "datetime"
      }
    ]
    ```

#### `GET /me`
Get your own active stories.
- **Response**: `List<StoryDto>`

#### `GET /users/{userId}`
Get stories of a specific contact.
- **Response**: `ContactStoriesDto`

#### `GET /archived`
Get your archived (expired or manually archived) stories.
- **Response**: `List<StoryDto>`

### 3.4 Interactions

#### `POST /{storyId}/view`
Mark a story as viewed.
- **Request Body**: `watchedSeconds` (int)

#### `GET /{storyId}/viewers`
Get the list of viewers for your own story.
- **Response**: `StoryViewersDto`

#### `POST /{storyId}/react`
React to a story with an emoji.
- **Request Body**: `emoji` (string)

#### `DELETE /{storyId}/react`
Remove your reaction from a story.

#### `POST /{storyId}/reply`
Reply to a story. This sends a direct message to the story owner.
- **Request Body**: `message` (string)

### 3.5 Privacy Settings

#### `GET /privacy`
Get your default story privacy settings.

#### `PUT /privacy`
Update your default story privacy settings.
- **Request Body (`UpdatePrivacySettingsRequest`):**
    ```json
    {
      "privacy": 1,
      "hiddenFromUserIds": [],
      "allowedUserIds": []
    }
    ```

---

## 4. WebSocket Events
The following events are pushed to the client via WebSockets from the Gateway.

### `new_story`
Sent to contacts when a user posts a new story.
- **Payload**: `StoryDto`

### `story_viewed`
Sent to the story owner when someone views their story.
- **Payload**: `StoryViewedEvent`
    ```json
    {
      "storyId": "string",
      "viewerId": "string",
      "ownerId": "string"
    }
    ```

### `story_reaction`
Sent to the story owner when someone reacts.
- **Payload**: `StoryReactionEvent`

### `story_reply`
Sent to the story owner when someone replies.
- **Payload**: `StoryReplyEvent`

### `story_expired`
Sent to contacts when a story is removed (expired or deleted).
- **Payload**: `StoryExpiredEvent`

---

## 5. Enums

### `StoryMediaType`
- `0`: Photo
- `1`: Video
- `2`: Text

### `StoryPrivacy`
- `0`: Everyone
- `1`: Contacts
- `2`: ContactsExcept (use `HiddenFromUserIds`)
- `3`: OnlyShareWith (use `AllowedUserIds`)

---

## 6. Implementation Notes
- **Story Duration**: Stories automatically expire after **24 hours**.
- **View Tracking**: Call the `view` endpoint as soon as the user starts watching the story.
- **Media Upload**: Use a standard `PUT` request with the binary data to the `presignedUrl` provided by the API.

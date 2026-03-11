# WhatsApp-like Stories Feature

The Stories feature allows users to share ephemeral media or text updates with their contacts. This document provides a technical overview of the implementation.

## 1. Core Concepts

- **Ephemeral**: Stories expire automatically after 24 hours.
- **Media Support**: Support for Images, Videos, and Text-with-background.
- **Privacy First**: Granular controls for each story or default settings.
- **Engagement**: Real-time reactions, views tracking, and private replies.

## 2. Technical Architecture

The feature is implemented across four microservices:

1.  **API Service**: Handles CRUD operations, privacy settings, and media upload coordination.
2.  **Broadcast Preparation Worker**: Manages the fan-out of story events to eligible contacts based on privacy rules.
3.  **Gateway Service**: Delivers real-time story notifications via WebSockets.
4.  **Worker Service**: Runs the background cleanup job to expire stories and delete associated media.

## 3. Data Models (MongoDB)

### Story
```json
{
  "_id": "ObjectId",
  "UserId": "ObjectId",
  "Type": "Photo | Video | Text",
  "MediaUrl": "string",
  "ThumbnailUrl": "string",
  "TextContent": "string",
  "TextColor": "string",
  "BackgroundColor": "string",
  "FontStyle": "string",
  "Duration": "number",
  "Privacy": "Everyone | Contacts | ContactsExcept | OnlyShareWith",
  "HiddenFromUserIds": ["ObjectId"],
  "AllowedUserIds": ["ObjectId"],
  "CreatedAt": "ISODate",
  "ExpiresAt": "ISODate",
  "IsDeleted": "boolean",
  "IsArchived": "boolean"
}
```

## 4. API Endpoints

| Method | Route | Description |
| :--- | :--- | :--- |
| POST | `/api/v1/stories/upload-url` | Generate a presigned URL for media upload. |
| POST | `/api/v1/stories/upload-url/confirm` | Confirm successful media upload. |
| POST | `/api/v1/stories` | Create a new story. |
| DELETE | `/api/v1/stories/{storyId}` | Soft-delete a story. |
| POST | `/api/v1/stories/{storyId}/archive` | Archive an expired story. |
| GET | `/api/v1/stories/me` | Get all active stories for the current user. |
| GET | `/api/v1/stories/feed` | Get the prioritized stories feed from contacts. |
| GET | `/api/v1/stories/users/{userId}` | Get active stories for a specific contact. |
| POST | `/api/v1/stories/{storyId}/view` | Mark a story as viewed. |
| GET | `/api/v1/stories/{storyId}/viewers` | Get the list of viewers for a story. |
| POST | `/api/v1/stories/{storyId}/react` | Add an emoji reaction to a story. |
| DELETE | `/api/v1/stories/{storyId}/react` | Remove a reaction. |
| POST | `/api/v1/stories/{storyId}/reply` | Reply to a story (sends a private message). |
| GET | `/api/v1/stories/privacy` | Get current story privacy settings. |
| PUT | `/api/v1/stories/privacy` | Update story privacy settings. |
| GET | `/api/v1/stories/archived` | Get archived stories. |

## 5. Real-time WebSocket Events

| Method | Payload | Description |
| :--- | :--- | :--- |
| `new_story` | `{ Story: StoryDto, OwnerId: string }` | Sent to eligible contacts when a new story is created. |
| `story_viewed` | `{ StoryId, ViewerId, ViewerName, ViewedAt }` | Sent to the owner when someone views their story. |
| `story_reaction` | `{ StoryId, UserId, UserName, Emoji, ReactedAt }` | Sent to the owner when someone reacts. |
| `story_reply` | `{ StoryId, SenderId, SenderName, Message, SentAt }` | Sent to the owner when someone replies. |
| `story_expired` | `{ StoryId, OwnerId }` | Sent to contacts when a story expires. |

## 6. Privacy Logic

The system strictly enforces privacy rules in both the Feed and direct User views:

-   **Everyone**: All contacts can see the story.
-   **Contacts**: Only users who are in the owner's contact list can see.
-   **Contacts Except**: Contacts who are NOT in the `HiddenFromUserIds` list can see.
-   **Only Share With**: Only users explicitly listed in `AllowedUserIds` can see.

## 7. Media Upload Strategy

1.  Client requests a presigned URL from `/upload-url`.
2.  API returns a secure PUT URL (Azure Blob Storage).
3.  Client uploads the file directly to storage.
4.  Client confirms the upload via `/confirm`.
5.  Client creates the story using the `UploadId`.

## 8. Background Cleanup

The `StoryCleanupWorker` (Worker service) runs every 30 minutes to:
1.  Identify stories where `ExpiresAt <= DateTime.UtcNow`.
2.  Set `IsDeleted = true`.
3.  Delete the media file from Azure Blob Storage.
4.  Broadcast `story_expired` event to contacts.

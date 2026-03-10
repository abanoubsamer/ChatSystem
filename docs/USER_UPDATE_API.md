# ChatSystem – User Profile Update API

This document outlines the API design for updating user personal information in the ChatSystem.
All updates require a valid JWT token for authentication.

---

## 1. API Endpoints

### 1.1 Update Username

* **Endpoint**: `PATCH /api/v1/user/update-username`
* **Method**: `PATCH`
* **Purpose**: Allows the user to change their unique username.
* **Request Body (JSON)**:

```json
{
  "username": "new_awesome_username"
}
```

* **Validation Rules**:

  * Required.
  * 3–50 characters.
  * Alphanumeric characters and underscores only.
  * Must be unique in the system.

---

### 1.2 Update Bio

* **Endpoint**: `PATCH /api/v1/user/update-bio`
* **Method**: `PATCH`
* **Purpose**: Updates the user's short biography or status.
* **Request Body (JSON)**:

```json
{
  "bio": "Software Engineer | Tech Enthusiast"
}
```

* **Validation Rules**:

  * Max 500 characters.

---

### 1.3 Update Password

* **Endpoint**: `PATCH /api/v1/user/update-password`
* **Method**: `PATCH`
* **Purpose**: Securely updates the user's password.
* **Request Body (JSON)**:

```json
{
  "currentPassword": "old_password_123",
  "newPassword": "Secure_New_Password_!99"
}
```

* **Validation Rules**:

  * `currentPassword`: Required.
  * `newPassword`: Required, must follow strong password policy:

    * Minimum 8 characters
    * Uppercase, lowercase, digit, special character
* **Security Considerations**:

  * Current password must be verified against the stored hash before applying changes.
  * Passwords must be hashed using BCrypt before storage.

---

### 1.4 Update Profile Picture (Avatar)

* **Endpoint**: `PATCH /api/v1/user/update-avatar`
* **Method**: `PATCH`
* **Purpose**: Updates the user's profile picture via a URL uploaded to cloud storage.
* **Request Body (JSON)**:

```json
{
  "avatarUrl": "https://mycloudstorage.com/uploads/avatars/user123.png"
}
```

* **Validation Rules**:

  * Required.
  * Must be a valid URL.
  * Optional: ensure the URL points to an image (e.g., `.jpg`, `.png`, `.webp`).

* **Backend Processing**:

  * Only updates the `AvatarUrl` field in the database.
  * Updates `UpdateTime` timestamp.

* **Frontend Flow**:

  1. Upload image to cloud storage (e.g., Firebase, AWS S3, Cloudinary).
  2. Receive image URL from the cloud service.
  3. Send PATCH request to API with `avatarUrl`.

---

## 2. Security & Best Practices

1. **JWT Authentication**: All endpoints require valid JWT token.
2. **Input Sanitization**: Sanitize username and bio to prevent XSS.
3. **Password Strength**: Enforce strong password policy.
4. **Idempotency & Auditing**: Consider logging sensitive changes (username, password, avatar URL).
5. **Data Validation**: All fields are validated before updating in database.

---

## 3. Database Update Strategy

* All updates also set the `UpdateTime` field to the current UTC time.
* Avatar updates only store the URL, no file uploads handled by backend.
* Ensures minimal backend load and reduces storage complexity.

---

## 4. Example Requests

### Update Username

```http
PATCH /api/v1/user/update-username
Content-Type: application/json
Authorization: Bearer <JWT_TOKEN>

{
  "username": "supercoder123"
}
```

### Update Bio

```http
PATCH /api/v1/user/update-bio
Content-Type: application/json
Authorization: Bearer <JWT_TOKEN>

{
  "bio": "Loves coding and coffee ☕"
}
```

### Update Password

```http
PATCH /api/v1/user/update-password
Content-Type: application/json
Authorization: Bearer <JWT_TOKEN>

{
  "currentPassword": "old_password_123",
  "newPassword": "New_Strong_Pass!99"
}
```

### Update Avatar

```http
PATCH /api/v1/user/update-avatar
Content-Type: appl
```

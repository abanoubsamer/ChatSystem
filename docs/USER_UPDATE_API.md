# User Profile Update API Design

This document outlines the API design for updating user personal information in the ChatSystem.

---

## 1. API Endpoints

All endpoints require a valid JWT token.

### 1.1 Update Username
- **Endpoint**: `PATCH /api/v1/user/update-username`
- **Method**: `PATCH`
- **Purpose**: Allows the user to change their unique username.
- **Request Body (JSON)**:
  ```json
  {
    "username": "new_awesome_username"
  }
  ```
- **Validation Rules**:
    - Required.
    - 3-50 characters.
    - Alphanumeric characters and underscores only.
    - Must be unique in the system.

### 1.2 Update Bio
- **Endpoint**: `PATCH /api/v1/user/update-bio`
- **Method**: `PATCH`
- **Purpose**: Updates the user's short biography or status.
- **Request Body (JSON)**:
  ```json
  {
    "bio": "Software Engineer | Tech Enthusiast"
  }
  ```
- **Validation Rules**:
    - Max 500 characters.

### 1.3 Update Password
- **Endpoint**: `PATCH /api/v1/user/update-password`
- **Method**: `PATCH`
- **Purpose**: Securely updates the user's password.
- **Request Body (JSON)**:
  ```json
  {
    "currentPassword": "old_password_123",
    "newPassword": "Secure_New_Password_!99"
  }
  ```
- **Validation Rules**:
    - `currentPassword`: Required.
    - `newPassword`: Required, must follow strong password policy (Min 8 chars, uppercase, lowercase, digit, special char).
- **Security Considerations**:
    - Current password must be verified against the stored hash before applying changes.
    - Passwords must be hashed using BCrypt before storage.

### 1.4 Update Profile Picture
- **Endpoint**: `PATCH /api/v1/user/update-avatar`
- **Method**: `PATCH`
- **Purpose**: Updates the user's profile picture URL.
- **Request Body (JSON)**:
  ```json
  {
    "avatarUrl": "https://cloud-storage.com/path/to/image.png"
  }
  ```
- **Validation Rules**:
    - Must be a valid URL.

---

## 2. Image Handling Strategy

### Storage Approach: Cloud Storage (Frontend Managed)
The frontend application is responsible for uploading image files to a cloud storage provider (e.g., AWS S3, Cloudinary, Azure Blob).

1.  **Frontend Flow**:
    - Frontend captures the image.
    - Frontend uploads the image directly to the cloud.
    - Cloud provider returns a permanent URL.
2.  **Backend Flow**:
    - Backend receives the URL via the `update-avatar` endpoint.
    - Backend persists the URL in the `AvatarUrl` field of the `AppUser` document.
3.  **Benefits**: Reduces backend load, simplifies scalability, and leverages specialized media processing features of cloud providers.

---

## 3. Security Best Practices

1.  **Token Validation**: All update endpoints must verify the user's identity via the JWT `NameIdentifier` claim.
2.  **Input Sanitization**: Bio and Username must be sanitized to prevent XSS.
3.  **Password Strength**: Strict password complexity requirements are enforced.
4.  **Idempotency & Auditing**: Consider logging sensitive changes (like password/username updates).

# Backend API Implementation - VietStart Chat & Invitation System

## ? Implementation Status: COMPLETED

This document describes the implemented API endpoints for the VietStart Chat and Member Recruitment system.

---

## ?? Complete Workflow

```
???????????????????????????????????????????????????????????????????
?                    INVITATION WORKFLOW                           ?
???????????????????????????????????????????????????????????????????

1. OWNER g?i l?i m?i
   POST /api/TeamStartUps/invite
   Body: { startUpId: number, userId: string }
   ? Status = Pending (0)

2. RECEIVER xem l?i m?i nh?n ???c
   GET /api/TeamStartUps/received-invites?status=0
   ? Tr? v? danh sách invitation v?i status Pending

3. RECEIVER ch?p nh?n l?i m?i
   PUT /api/TeamStartUps/{id}/accept-invite
   ? Status = Dealing (1)
   ? B?t ??u chat riêng 1-1 trên Firebase

4. OWNER và RECEIVER trao ??i qua private chat
   (Firebase Firestore: privateChatRooms & privateMessages)

5. OWNER hoàn t?t chiêu m?
   PUT /api/TeamStartUps/{id}/confirm-success
   ? Status = Success (2)
   ? Frontend t?o group chat room trên Firebase

6. Các tr??ng h?p khác:
   - RECEIVER t? ch?i: PUT /api/TeamStartUps/{id}/reject-invite ? Rejected (3)
   - OWNER h?y Pending: DELETE /api/TeamStartUps/{id}/cancel-invite
   - OWNER h?y Dealing: PUT /api/TeamStartUps/{id}/cancel-dealing ? Rejected (3)
```

---

## ?? Status Enum

```csharp
public enum TeamStartUpStatus
{
    Pending = 0,   // ?ang ch? ph?n h?i
    Dealing = 1,   // ?ang trao ??i
    Success = 2,   // Thành công (?ã là member)
    Rejected = 3   // B? t? ch?i ho?c h?y
}
```

---

## ?? API Endpoints - Implemented

### 1. ? POST /api/TeamStartUps/invite
**Mô t?**: Owner g?i l?i m?i chiêu m? cho user

**Request Body**:
```json
{
  "startUpId": 123,
  "userId": "f45d2836-b604-4ba5-ad4a-ac63bcaa2aa8"
}
```

**Response Success (200)**:
```json
{
  "message": "G?i l?i m?i chiêu m? thành công"
}
```

**Error Responses**:
- 400: "StartUp không t?n t?i"
- 400: "User không t?n t?i"
- 400: "?ã có l?i m?i ?ang ch? x? lý"
- 400: "?ang trong quá trình trao ??i v?i user này"
- 400: "User ?ã là thành viên c?a startup này"
- 401: "Không xác ??nh ???c ng??i dùng"
- 403: "B?n không có quy?n g?i l?i m?i cho startup này"

---

### 2. ? GET /api/TeamStartUps/sent-invites
**Mô t?**: L?y danh sách l?i m?i ?ã g?i (owner view)

**Query Parameters**:
- `startUpId` (optional): number - Filter theo startup c? th?
- `status` (optional): number (0-3) - Filter theo status

**Example Request**:
```
GET /api/TeamStartUps/sent-invites?status=1
```

**Response Success (200)**:
```json
{
  "data": [
    {
      "id": 456,
      "startUpId": 123,
      "startUpIdea": "AI for Education",
      "userId": "f45d2836-b604-4ba5-ad4a-ac63bcaa2aa8",
      "userFullName": "Nguy?n V?n A",
      "userAvatar": "https://...",
      "status": 1,
      "startupOwnerId": "owner-uuid-here",
      "startupOwnerName": "Nguy?n V?n B",
      "startupOwnerAvatar": "https://..."
    }
  ],
  "total": 1
}
```

---

### 3. ? GET /api/TeamStartUps/received-invites
**Mô t?**: L?y danh sách l?i m?i nh?n ???c (receiver view)

**Query Parameters**:
- `status` (optional): number (0-3) - Filter theo status

**Example Request**:
```
GET /api/TeamStartUps/received-invites?status=0
```

**Response Success (200)**:
```json
{
  "data": [
    {
      "id": 456,
      "startUpId": 123,
      "startUpIdea": "AI for Education",
      "userId": "f45d2836-b604-4ba5-ad4a-ac63bcaa2aa8",
      "userFullName": "Nguy?n V?n A",
      "userAvatar": "https://...",
      "status": 0,
      "startupOwnerId": "owner-uuid-here",
      "startupOwnerName": "Nguy?n V?n B",
      "startupOwnerAvatar": "https://..."
    }
  ],
  "total": 1
}
```

---

### 4. ? PUT /api/TeamStartUps/{id}/accept-invite
**Mô t?**: Receiver ch?p nh?n l?i m?i ? chuy?n sang Dealing

**URL Parameter**:
- `id`: number - Invitation ID

**Response Success (200)**:
```json
{
  "message": "?ã ch?p nh?n l?i m?i. Bây gi? b?n có th? nh?n tin trao ??i v?i ch? startup",
  "status": 1
}
```

**Error Responses**:
- 400: "L?i m?i này không ? tr?ng thái ch? x? lý"
- 401: "Không xác ??nh ???c ng??i dùng"
- 403: "B?n không có quy?n ch?p nh?n l?i m?i này"
- 404: "L?i m?i không t?n t?i"

---

### 5. ? PUT /api/TeamStartUps/{id}/reject-invite
**Mô t?**: Receiver t? ch?i l?i m?i

**URL Parameter**:
- `id`: number - Invitation ID

**Request Body (optional)**:
```json
{
  "reason": "Không phù h?p v?i l?ch trình"
}
```

**Response Success (200)**:
```json
{
  "message": "?ã t? ch?i l?i m?i",
  "reason": "Không phù h?p v?i l?ch trình",
  "status": 3
}
```

---

### 6. ? PUT /api/TeamStartUps/{id}/confirm-success
**Mô t?**: Owner xác nh?n thành công ? thêm member vào group

**URL Parameter**:
- `id`: number - Invitation ID

**Response Success (200)**:
```json
{
  "message": "?ã xác nh?n thành công. Thành viên ?ã ???c thêm vào nhóm chat",
  "status": 2
}
```

**Error Responses**:
- 400: "Ch? có th? xác nh?n thành công khi ?ang ? tr?ng thái Dealing"
- 401: "Không xác ??nh ???c ng??i dùng"
- 403: "B?n không có quy?n xác nh?n l?i m?i này"
- 404: "L?i m?i không t?n t?i"

---

### 7. ? PUT /api/TeamStartUps/{id}/cancel-dealing
**Mô t?**: Owner h?y b? quá trình trao ??i

**URL Parameter**:
- `id`: number - Invitation ID

**Request Body (optional)**:
```json
{
  "reason": "Không phù h?p sau khi trao ??i"
}
```

**Response Success (200)**:
```json
{
  "message": "?ã h?y b? quá trình trao ??i",
  "reason": "Không phù h?p sau khi trao ??i",
  "status": 3
}
```

---

### 8. ? DELETE /api/TeamStartUps/{id}/cancel-invite
**Mô t?**: Owner h?y l?i m?i (ch? khi Pending)

**URL Parameter**:
- `id`: number - Invitation ID

**Response Success (200)**:
```json
{
  "message": "?ã h?y l?i m?i"
}
```

**Error Responses**:
- 400: "Ch? có th? h?y l?i m?i khi còn ? tr?ng thái Pending"
- 401: "Không xác ??nh ???c ng??i dùng"
- 403: "B?n không có quy?n h?y l?i m?i này"
- 404: "L?i m?i không t?n t?i"

---

### 9. ? DELETE /api/TeamStartUps/{id}/remove-member
**Mô t?**: Owner xóa thành viên kh?i startup

**URL Parameter**:
- `id`: number - TeamStartUp ID (status = Success)

**Response Success (200)**:
```json
{
  "message": "?ã xóa thành viên kh?i startup"
}
```

---

### 10. ? GET /api/TeamStartUps/dealing-chats
**Mô t?**: L?y danh sách các invitation ?ang Dealing (c? owner và receiver)

**Response Success (200)**:
```json
{
  "data": [
    {
      "id": 456,
      "startUpId": 123,
      "startUpIdea": "AI for Education",
      "userId": "receiver-id",
      "userFullName": "Nguy?n V?n A",
      "userAvatar": "https://...",
      "status": 1,
      "startupOwnerId": "owner-id",
      "startupOwnerName": "Nguy?n V?n B",
      "startupOwnerAvatar": "https://..."
    }
  ],
  "total": 1
}
```

---

### 11. ? GET /api/TeamStartUps/my-team-members
**Mô t?**: L?y danh sách thành viên Success c?a startup

**Query Parameters**:
- `startUpId` (optional): number

**Response Success (200)**:
```json
{
  "data": [
    {
      "id": 456,
      "startUpId": 123,
      "startUpIdea": "AI for Education",
      "userId": "member-id",
      "userFullName": "Nguy?n V?n A",
      "userAvatar": "https://...",
      "status": 2,
      "startupOwnerId": "owner-id",
      "startupOwnerName": "Nguy?n V?n B",
      "startupOwnerAvatar": "https://..."
    }
  ],
  "total": 1
}
```

---

## ? Implementation Checklist

### Core Features
- [x] TeamStartUpStatus enum (0-3)
- [x] TeamStartUp entity updated with enum
- [x] TeamStartUpDto with owner information
- [x] All 11 API endpoints implemented
- [x] Proper authorization and validation
- [x] Descriptive error messages
- [x] Consistent response format

### DTO Updates
- [x] `StartupOwnerId` field added
- [x] `StartupOwnerName` field added
- [x] `StartupOwnerAvatar` field added
- [x] Status as enum (number 0-3)

### Repository Updates
- [x] Interface updated for enum
- [x] Implementation updated for enum
- [x] Include StartUp and User relationships
- [x] Order by Id descending

### Controller Updates
- [x] All endpoints accept/return enum (number)
- [x] Owner information populated in responses
- [x] Proper 401, 403, 404, 400 error handling
- [x] Consistent error message format
- [x] JWT token validation

---

## ?? Key Implementation Details

### 1. Status Parameter Handling
? Controller methods accept `TeamStartUpStatus?` enum parameter
? Frontend sends: `?status=0`, `?status=1`, etc.
? Backend automatically parses to enum

### 2. Owner Information
? All DTOs include owner details
? Fetched from StartUp.UserId relationship
? Used for Firebase chat room creation

### 3. Security
? JWT Authorization required
? Role-based access (Client role)
? Ownership verification on all operations
? User identity from Claims

### 4. Validation
? Duplicate invitation check
? Status transition validation
? Entity existence validation
? Proper error messages

---

## ?? Frontend Integration

### Status Mapping
```javascript
const STATUS = {
  PENDING: 0,
  DEALING: 1,
  SUCCESS: 2,
  REJECTED: 3
}

// API call example
GET /api/TeamStartUps/received-invites?status=0  // Pending
GET /api/TeamStartUps/sent-invites?status=1      // Dealing
```

### Private Chat Room ID Format
```javascript
const roomId = `private_invitation_${invitationId}_${userId1}_${userId2}`;
```

---

## ?? Testing Scenarios

### ? Scenario 1: Happy Path
1. Owner g?i invitation ? Status = Pending
2. Receiver xem /received-invites ? Th?y invitation
3. Receiver accept ? Status = Dealing
4. C? 2 chat trên Firebase
5. Owner confirm success ? Status = Success
6. Member ???c add vào group chat

### ? Scenario 2: Rejection Path
1. Owner g?i invitation ? Pending
2. Receiver reject ? Rejected

### ? Scenario 3: Owner Cancel
1. Owner g?i invitation ? Pending
2. Owner cancel-invite ? Deleted

### ? Scenario 4: Edge Cases
1. G?i duplicate invitation ? 400 error
2. User không t?n t?i ? 400 error
3. Accept invitation c?a ng??i khác ? 403 error
4. Confirm success khi status = Pending ? 400 error

---

## ?? Database Migration

To apply changes to database:
```bash
dotnet ef migrations add UpdateTeamStartUpStatusToEnum
dotnet ef database update
```

---

## ?? Notes

- Frontend s? d?ng Firebase cho chat (private & group)
- Backend ch? qu?n lý invitation status
- Owner information ???c include trong t?t c? responses
- Status parameters ??u là number (0-3)

---

**Document Version**: 2.0 (Implemented)  
**Last Updated**: November 23, 2025  
**Status**: ? COMPLETED & TESTED

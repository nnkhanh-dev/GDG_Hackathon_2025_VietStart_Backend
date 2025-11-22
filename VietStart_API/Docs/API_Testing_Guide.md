# API Testing Guide - TeamStartUps

## Quick Test Commands (using curl or Postman)

### Prerequisites
- Get JWT token from login endpoint
- Replace `{token}` with your actual JWT token
- Replace `{userId}` with actual user ID
- Replace `{startUpId}` with actual startup ID

---

## 1. Send Invitation (Owner)

```bash
POST http://localhost:5000/api/TeamStartUps/invite
Headers:
  Authorization: Bearer {token}
  Content-Type: application/json

Body:
{
  "startUpId": 1,
  "userId": "f45d2836-b604-4ba5-ad4a-ac63bcaa2aa8"
}
```

**Expected**: 200 OK with success message

---

## 2. Get Sent Invitations (Owner)

```bash
GET http://localhost:5000/api/TeamStartUps/sent-invites
Headers:
  Authorization: Bearer {token}

# With filters
GET http://localhost:5000/api/TeamStartUps/sent-invites?status=0&startUpId=1
```

**Expected**: 200 OK with list of invitations including owner info

---

## 3. Get Received Invitations (Receiver)

```bash
GET http://localhost:5000/api/TeamStartUps/received-invites
Headers:
  Authorization: Bearer {token}

# Filter by status
GET http://localhost:5000/api/TeamStartUps/received-invites?status=0
```

**Expected**: 200 OK with list of invitations including owner info

---

## 4. Accept Invitation (Receiver)

```bash
PUT http://localhost:5000/api/TeamStartUps/456/accept-invite
Headers:
  Authorization: Bearer {token}
```

**Expected**: 200 OK, status changes to Dealing (1)

---

## 5. Reject Invitation (Receiver)

```bash
PUT http://localhost:5000/api/TeamStartUps/456/reject-invite
Headers:
  Authorization: Bearer {token}
  Content-Type: application/json

Body (optional):
{
  "reason": "Không phù h?p v?i l?ch trình"
}
```

**Expected**: 200 OK, status changes to Rejected (3)

---

## 6. Confirm Success (Owner)

```bash
PUT http://localhost:5000/api/TeamStartUps/456/confirm-success
Headers:
  Authorization: Bearer {token}
```

**Expected**: 200 OK, status changes to Success (2)

---

## 7. Cancel Dealing (Owner)

```bash
PUT http://localhost:5000/api/TeamStartUps/456/cancel-dealing
Headers:
  Authorization: Bearer {token}
  Content-Type: application/json

Body (optional):
{
  "reason": "Không phù h?p sau khi trao ??i"
}
```

**Expected**: 200 OK, status changes to Rejected (3)

---

## 8. Cancel Invitation (Owner, Pending only)

```bash
DELETE http://localhost:5000/api/TeamStartUps/456/cancel-invite
Headers:
  Authorization: Bearer {token}
```

**Expected**: 200 OK, invitation deleted

---

## 9. Get Dealing Chats (Both)

```bash
GET http://localhost:5000/api/TeamStartUps/dealing-chats
Headers:
  Authorization: Bearer {token}
```

**Expected**: 200 OK with list of all Dealing invitations

---

## 10. Get Team Members (Owner)

```bash
GET http://localhost:5000/api/TeamStartUps/my-team-members
Headers:
  Authorization: Bearer {token}

# Filter by startup
GET http://localhost:5000/api/TeamStartUps/my-team-members?startUpId=1
```

**Expected**: 200 OK with list of Success members

---

## 11. Remove Member (Owner)

```bash
DELETE http://localhost:5000/api/TeamStartUps/456/remove-member
Headers:
  Authorization: Bearer {token}
```

**Expected**: 200 OK, member removed

---

## Status Values

| Status | Value | Description |
|--------|-------|-------------|
| Pending | 0 | ?ang ch? ph?n h?i |
| Dealing | 1 | ?ang trao ??i |
| Success | 2 | Thành công |
| Rejected | 3 | T? ch?i |

---

## Common Error Codes

| Code | Message |
|------|---------|
| 400 | Bad Request - Invalid data or status transition |
| 401 | Unauthorized - No token or invalid token |
| 403 | Forbidden - Not owner or not receiver |
| 404 | Not Found - Entity doesn't exist |

---

## Test Flow Example

1. **Owner** sends invitation ? Status = Pending (0)
2. **Receiver** views received-invites ? Sees invitation
3. **Receiver** accepts ? Status = Dealing (1)
4. **Both** see in dealing-chats
5. **Owner** confirms success ? Status = Success (2)
6. **Owner** views my-team-members ? Sees new member

---

## Postman Collection

Import this collection to Postman for quick testing:

```json
{
  "info": {
    "name": "VietStart TeamStartUps API",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Send Invitation",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{token}}"
          }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"startUpId\": 1,\n  \"userId\": \"user-id-here\"\n}",
          "options": {
            "raw": {
              "language": "json"
            }
          }
        },
        "url": {
          "raw": "{{baseUrl}}/api/TeamStartUps/invite",
          "host": ["{{baseUrl}}"],
          "path": ["api", "TeamStartUps", "invite"]
        }
      }
    }
  ]
}
```

---

## Environment Variables (Postman)

```
baseUrl: http://localhost:5000
token: your-jwt-token-here
```

# Backend Implementation Checklist

## ? Completed Tasks

### 1. Enum & Entities
- [x] `TeamStartUpStatus` enum created (Pending=0, Dealing=1, Success=2, Rejected=3)
- [x] `TeamStartUp` entity updated to use enum
- [x] Database migration ready

### 2. DTOs
- [x] `TeamStartUpDto` updated with:
  - [x] `StartupOwnerId` field
  - [x] `StartupOwnerName` field
  - [x] `StartupOwnerAvatar` field
  - [x] `Status` as enum type
- [x] `CreateTeamStartUpDto` simplified (removed Status field - always Pending)
- [x] `UpdateJoinRequestStatusDto` uses enum

### 3. Repository Layer
- [x] `ITeamStartUpRepository` interface updated for enum
- [x] `TeamStartUpRepository` implementation updated:
  - [x] `GetTeamStartUpsByStatusAsync()` accepts enum
  - [x] All methods include necessary relationships (User, StartUp)
  - [x] Results ordered by Id descending

### 4. Controller Endpoints (11 total)

#### Owner Endpoints
- [x] `POST /api/TeamStartUps/invite` - Send invitation
- [x] `GET /api/TeamStartUps/sent-invites` - View sent invitations
- [x] `DELETE /api/TeamStartUps/{id}/cancel-invite` - Cancel pending invitation
- [x] `PUT /api/TeamStartUps/{id}/confirm-success` - Confirm success (Dealing ? Success)
- [x] `PUT /api/TeamStartUps/{id}/cancel-dealing` - Cancel dealing (Dealing ? Rejected)
- [x] `GET /api/TeamStartUps/my-team-members` - View team members
- [x] `DELETE /api/TeamStartUps/{id}/remove-member` - Remove member

#### Receiver Endpoints
- [x] `GET /api/TeamStartUps/received-invites` - View received invitations
- [x] `PUT /api/TeamStartUps/{id}/accept-invite` - Accept invitation (Pending ? Dealing)
- [x] `PUT /api/TeamStartUps/{id}/reject-invite` - Reject invitation (Pending ? Rejected)

#### Both Endpoints
- [x] `GET /api/TeamStartUps/dealing-chats` - View all dealing conversations

### 5. Validation & Security
- [x] JWT Authorization on all endpoints
- [x] Role-based access control (Client role)
- [x] Ownership verification:
  - [x] Owner can only manage own startups
  - [x] Receiver can only respond to own invitations
- [x] Duplicate invitation check
- [x] Status transition validation
- [x] Entity existence validation

### 6. Response Format
- [x] Success responses with proper format: `{ message, data?, total?, status? }`
- [x] Error responses with descriptive messages
- [x] Proper HTTP status codes: 200, 400, 401, 403, 404

### 7. Owner Information Inclusion
- [x] `sent-invites` includes owner info
- [x] `received-invites` includes owner info
- [x] `dealing-chats` includes owner info
- [x] `my-team-members` includes owner info

### 8. Status Parameter Handling
- [x] Controllers accept `TeamStartUpStatus?` enum parameter
- [x] Query string `?status=0` correctly parses to enum
- [x] No string-based status handling

### 9. Documentation
- [x] `TeamStartUpFlow.md` - Complete workflow and API details
- [x] `API_Testing_Guide.md` - Testing instructions
- [x] This checklist file

### 10. Code Quality
- [x] No compilation errors
- [x] Build successful
- [x] Consistent naming conventions
- [x] Proper async/await usage
- [x] Include necessary relationships to avoid N+1 queries

---

## ?? Important Notes for Frontend

### 1. Status Values
Frontend MUST send status as **numbers**, not strings:
- ? `?status=0` (Pending)
- ? `?status=1` (Dealing)
- ? `?status=Pending` (Wrong!)

### 2. Owner Information
All responses now include:
```json
{
  "startupOwnerId": "uuid-here",
  "startupOwnerName": "Full Name",
  "startupOwnerAvatar": "url-here"
}
```

### 3. Firebase Integration
Backend does NOT handle Firebase chat creation. Frontend should:
- Create private chat room when status = Dealing
- Create group chat room when status = Success
- Use invitationId and owner info to create proper room IDs

### 4. Authorization
All API calls MUST include JWT token:
```
Authorization: Bearer {token}
```

---

## ?? Workflow Validation

### State Machine
```
Pending (0)
  ?? [Receiver Accept] ? Dealing (1)
  ?    ?? [Owner Confirm] ? Success (2) ?
  ?    ?? [Owner Cancel] ? Rejected (3) ?
  ?? [Receiver Reject] ? Rejected (3) ?
  ?? [Owner Cancel] ? Deleted ?
```

### Valid Transitions
- [x] Pending ? Dealing (accept-invite)
- [x] Pending ? Rejected (reject-invite)
- [x] Pending ? Deleted (cancel-invite)
- [x] Dealing ? Success (confirm-success)
- [x] Dealing ? Rejected (cancel-dealing)

### Invalid Transitions (Blocked)
- [x] Pending ? Success (must go through Dealing)
- [x] Dealing ? Pending (cannot go back)
- [x] Success ? Any (final state)
- [x] Rejected ? Any (final state)

---

## ?? Deployment Checklist

Before deploying to production:
- [ ] Run database migration: `dotnet ef database update`
- [ ] Test all 11 endpoints manually or with Postman
- [ ] Verify JWT authentication works
- [ ] Check all error responses return proper messages
- [ ] Confirm owner information is included in all responses
- [ ] Test with multiple users (owner and receiver roles)
- [ ] Load test with multiple concurrent requests
- [ ] Verify no N+1 query issues (use EF Core logging)

---

## ?? Known Limitations / TODO

1. **Firebase Integration**
   - Backend does NOT create Firebase chat rooms
   - Frontend must handle chat room creation
   - Consider adding webhook to notify frontend when status changes

2. **Notifications**
   - No email/push notification implemented
   - Consider adding notification service for:
     - New invitation received
     - Invitation accepted
     - Invitation rejected
     - Member confirmed

3. **Soft Delete**
   - Currently using hard delete
   - Consider soft delete for audit trail

4. **Pagination**
   - No pagination implemented
   - May need for startups with many invitations

---

## ?? Performance Considerations

### Current Implementation
- ? Eager loading with `.Include()` to avoid N+1 queries
- ? Single database query per endpoint (mostly)
- ? No unnecessary data fetching

### Potential Improvements
- [ ] Add response caching for frequently accessed data
- [ ] Implement pagination for large lists
- [ ] Add indexes on Status and UserId columns
- [ ] Consider Redis for real-time invitation counts

---

## ?? Migration Command

```bash
# Create migration
dotnet ef migrations add UpdateTeamStartUpStatusToEnum --project VietStart_API

# Apply to database
dotnet ef database update --project VietStart_API

# Rollback if needed
dotnet ef database update PreviousMigrationName --project VietStart_API
```

---

**Status**: ? READY FOR FRONTEND INTEGRATION  
**Last Updated**: November 23, 2025  
**Reviewed By**: Backend Team

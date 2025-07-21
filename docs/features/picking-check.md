# Picking Check Feature

## Overview

The Picking Check feature allows warehouse supervisors to initiate quality control checks on completed or partially completed pick lists. This feature enables verification of picked quantities against what was actually collected, helping identify discrepancies before shipment.

## Key Features

- **Supervisor-Initiated Checks**: Picking supervisors can start a check process on any pick list
- **Role-Based Access**: New `PickingCheck` role for users who perform verification
- **Real-Time Discrepancy Tracking**: Immediate visibility of quantity differences
- **In-Memory Session Management**: Temporary check data stored in memory (not persisted to database)
- **Support for Partial Picks**: Can verify both complete and partial pick lists

## User Roles

### Picking Supervisor (RoleType.PickingSupervisor)
- View all pick lists (including completed ones)
- Start check sessions
- Complete check sessions
- View check summaries

### Picking Checker (RoleType.PickingCheck)
- Perform item checks using barcode scanner
- View check progress and summaries
- Cannot start or complete check sessions

## Workflow

1. **Supervisor Initiates Check**
   - Navigate to Picking Supervisor view
   - Select a pick list to check
   - Click "Start Check" button

2. **Checker Performs Verification**
   - Access the check process via `/pick/{id}/check`
   - Scan items using barcode scanner
   - System displays real-time progress and discrepancies

3. **Review Discrepancies**
   - View summary showing:
     - Items checked vs total items
     - Quantity discrepancies (both over and under)
     - Color-coded status indicators

4. **Complete Check**
   - Supervisor reviews final summary
   - Completes the check process
   - Data expires after 24 hours

## Technical Implementation

### Backend Components

- **Controller**: `PickingController` with 4 new endpoints
- **Service**: `PickListCheckService` for business logic
- **Storage**: `IMemoryCache` for temporary session data
- **DTOs**: Request/response models in `Core.DTOs.PickList`

### Frontend Components

- **Component**: `picking-check.tsx` for check UI
- **Service**: Updated `picking-service.ts` with check methods
- **Translations**: Added support for English and Spanish

### API Endpoints

```
POST /api/picking/{id}/check/start     - Start check session (Supervisor only)
POST /api/picking/{id}/check/item      - Check an item
GET  /api/picking/{id}/check/summary   - Get check summary
POST /api/picking/{id}/check/complete  - Complete check (Supervisor only)
```

## Configuration

### Session Timeouts
- Active sessions: 4 hours
- Completed sessions: 24 hours

### Memory Cache
Sessions are stored in-memory using ASP.NET Core's `IMemoryCache` with sliding expiration.

## Limitations

1. **No Database Persistence**: Check data is temporary and not saved
2. **No SAP Integration**: Results are not sent back to SAP
3. **Single Warehouse**: Checks are limited to current warehouse context
4. **No Historical Reports**: Past check data is not available

## Error Handling

- Invalid pick list IDs return appropriate error messages
- Missing sessions are handled gracefully
- Authorization failures return 403 Forbidden
- Concurrent access to same pick list is supported

## Future Enhancements

1. **Database Persistence**: Save check results for historical reporting
2. **SAP Integration**: Send discrepancy reports back to SAP
3. **Email Notifications**: Alert supervisors of significant discrepancies
4. **Mobile Optimization**: Enhanced mobile UI for warehouse floor use
5. **Batch Checking**: Check multiple pick lists simultaneously
6. **Photo Evidence**: Capture images of discrepancies

## Testing

### Unit Tests
- `PickListCheckServiceTests.cs` - Core service logic
- `PickListCheckServiceEdgeCaseTests.cs` - Edge cases and error scenarios
- `PickingCheckAuthorizationTests.cs` - Role verification

### Integration Tests
- `PickListCheckTests.cs` - End-to-end API testing

### Manual Testing
- `PickingCheck.http` - HTTP requests for manual testing

## Troubleshooting

### Common Issues

1. **"No active check session"**
   - Session has expired (4-hour timeout)
   - Supervisor needs to start a new session

2. **403 Forbidden Errors**
   - User lacks required role
   - Verify user has PickingSupervisor or PickingCheck role

3. **Item Not Found**
   - Item code doesn't exist in pick list
   - Verify correct pick list is being checked

4. **Frontend Build Errors**
   - Case sensitivity issues on Windows
   - Set `forceConsistentCasingInFileNames: false` in tsconfig.json
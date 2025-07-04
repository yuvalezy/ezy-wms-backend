# Mock Cloud License Server

This directory contains a mock cloud license server for testing Phase 3 cloud integration features.

## Setup

1. Install Node.js dependencies:
```bash
cd scripts
npm install
```

2. Start the mock server:
```bash
npm start
# or
node mock-cloud-server.js
```

The server will start on port 3001 by default.

## Configuration

Update your `appsettings.json` to use the mock server:

```json
{
  "Licensing": {
    "CloudEndpoint": "http://localhost:3001",
    "BearerToken": "test-bearer-token-12345"
  },
  "BackgroundServices": {
    "CloudSync": {
      "SyncIntervalMinutes": 1,
      "ValidationIntervalHours": 1,
      "Enabled": true
    }
  }
}
```

## Endpoints

- `GET /api/license/health` - Health check
- `POST /api/license/device-event` - Device event handling
- `POST /api/license/validate-account` - Account validation
- `GET /api/debug/devices` - View registered devices (debug)

## Testing Scenarios

### Device Registration
The mock server tracks device registrations and status changes.

### Account Validation
- **Active**: When device count ≤ 5
- **PaymentDue**: When device count > 5 (simulates license exceeded)
- Devices beyond the limit are automatically marked for deactivation

### Device Limits
- Max allowed devices: 5
- Excess devices are marked for deactivation during validation

## Example Usage

Test device registration:
```bash
curl -X POST http://localhost:3001/api/license/device-event \
  -H "Authorization: Bearer test-bearer-token-12345" \
  -H "Content-Type: application/json" \
  -d '{
    "deviceUuid": "device-001",
    "event": "register",
    "deviceName": "Test Device",
    "timestamp": "2024-01-01T00:00:00Z"
  }'
```

Test account validation:
```bash
curl -X POST http://localhost:3001/api/license/validate-account \
  -H "Authorization: Bearer test-bearer-token-12345" \
  -H "Content-Type: application/json" \
  -d '{
    "activeDeviceUuids": ["device-001", "device-002"],
    "lastValidationTimestamp": "2024-01-01T00:00:00Z"
  }'
```

Check registered devices:
```bash
curl http://localhost:3001/api/debug/devices
```
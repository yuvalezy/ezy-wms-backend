#!/usr/bin/env node
/**
 * Mock Cloud License Server
 * 
 * This is a simple Express.js server that simulates the cloud licensing service
 * for testing Phase 3 cloud integration features.
 * 
 * Usage:
 *   node mock-cloud-server.js [port]
 * 
 * Default port: 3001
 * 
 * Endpoints:
 *   GET  /api/license/health - Health check
 *   POST /api/license/device-event - Device event handling
 *   POST /api/license/validate-account - Account validation
 * 
 * Configuration:
 *   - Bearer token: test-bearer-token-12345
 *   - Account status simulation based on device count
 *   - Device deactivation simulation for excess devices
 */

const express = require('express');
const cors = require('cors');
const app = express();
const port = process.argv[2] || 3001;

// Middleware
app.use(cors());
app.use(express.json());

// Mock data store
const mockData = {
    devices: new Map(),
    accountInfo: {
        status: 'Active',
        expirationDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString(), // 30 days from now
        paymentCycleDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString(),
        demoExpirationDate: null,
        maxAllowedDevices: 5,
        inactiveReason: null,
        additionalData: {
            planName: 'Professional',
            company: 'Mock Company Ltd'
        }
    }
};

// Bearer token validation middleware
function validateBearerToken(req, res, next) {
    const authHeader = req.headers.authorization;
    if (!authHeader || !authHeader.startsWith('Bearer ')) {
        return res.status(401).json({
            success: false,
            message: 'Missing or invalid authorization header'
        });
    }
    
    const token = authHeader.substring(7);
    if (token !== 'test-bearer-token-12345') {
        return res.status(401).json({
            success: false,
            message: 'Invalid bearer token'
        });
    }
    
    next();
}

// Health check endpoint
app.get('/api/license/health', (req, res) => {
    res.json({
        success: true,
        message: 'Mock cloud license server is running',
        timestamp: new Date().toISOString(),
        version: '1.0.0'
    });
});

// Device event endpoint
app.post('/api/license/device-event', validateBearerToken, (req, res) => {
    const { deviceUuid, event, deviceName, timestamp, additionalData } = req.body;
    
    if (!deviceUuid || !event) {
        return res.status(400).json({
            success: false,
            message: 'Device UUID and event are required'
        });
    }
    
    console.log(`📱 Device Event: ${event} for ${deviceUuid} (${deviceName || 'unnamed'})`);
    
    // Update mock device registry
    const device = mockData.devices.get(deviceUuid) || {
        deviceUuid,
        deviceName: deviceName || 'Unknown Device',
        registrationDate: new Date().toISOString(),
        lastEventDate: null,
        events: []
    };
    
    device.deviceName = deviceName || device.deviceName;
    device.lastEventDate = timestamp || new Date().toISOString();
    device.events.push({
        event,
        timestamp: timestamp || new Date().toISOString(),
        additionalData
    });
    
    // Handle different event types
    switch (event) {
        case 'register':
            device.status = 'active';
            console.log(`   ✅ Device ${deviceUuid} registered`);
            break;
        case 'activate':
            device.status = 'active';
            console.log(`   ✅ Device ${deviceUuid} activated`);
            break;
        case 'deactivate':
            device.status = 'inactive';
            console.log(`   ⏸️  Device ${deviceUuid} deactivated`);
            break;
        case 'disable':
            device.status = 'disabled';
            console.log(`   ❌ Device ${deviceUuid} disabled`);
            break;
        default:
            console.log(`   ℹ️  Device ${deviceUuid} event: ${event}`);
    }
    
    mockData.devices.set(deviceUuid, device);
    
    res.json({
        success: true,
        message: `Device event '${event}' processed successfully`,
        timestamp: new Date().toISOString(),
        deviceStatus: device.status
    });
});

// Account validation endpoint
app.post('/api/license/validate-account', validateBearerToken, (req, res) => {
    const { activeDeviceUuids, lastValidationTimestamp } = req.body;
    
    console.log(`🔍 Account Validation: ${activeDeviceUuids?.length || 0} active devices`);
    
    const activeDeviceCount = activeDeviceUuids?.length || 0;
    const maxAllowed = mockData.accountInfo.maxAllowedDevices;
    
    // Simulate account status based on device count
    let accountStatus = 'Active';
    let devicesToDeactivate = [];
    
    if (activeDeviceCount > maxAllowed) {
        accountStatus = 'PaymentDue';
        // Simulate deactivating excess devices
        devicesToDeactivate = activeDeviceUuids.slice(maxAllowed);
        console.log(`   ⚠️  Too many devices (${activeDeviceCount}/${maxAllowed}), marking as PaymentDue`);
        console.log(`   📱 Devices to deactivate: ${devicesToDeactivate.join(', ')}`);
    } else {
        console.log(`   ✅ Account valid (${activeDeviceCount}/${maxAllowed} devices)`);
    }
    
    // Update account info
    mockData.accountInfo.status = accountStatus;
    
    const response = {
        success: true,
        message: 'Account validation completed',
        licenseData: {
            accountStatus,
            expirationDate: mockData.accountInfo.expirationDate,
            paymentCycleDate: mockData.accountInfo.paymentCycleDate,
            demoExpirationDate: mockData.accountInfo.demoExpirationDate,
            inactiveReason: accountStatus === 'PaymentDue' ? 'Too many active devices' : null,
            activeDeviceCount,
            maxAllowedDevices: maxAllowed,
            additionalData: mockData.accountInfo.additionalData
        },
        devicesToDeactivate,
        timestamp: new Date().toISOString()
    };
    
    res.json(response);
});

// Get mock data (for debugging)
app.get('/api/debug/devices', (req, res) => {
    res.json({
        devices: Array.from(mockData.devices.values()),
        accountInfo: mockData.accountInfo
    });
});

// Error handling middleware
app.use((err, req, res, next) => {
    console.error('❌ Server error:', err);
    res.status(500).json({
        success: false,
        message: 'Internal server error',
        error: err.message
    });
});

// 404 handler
app.use((req, res) => {
    res.status(404).json({
        success: false,
        message: 'Endpoint not found'
    });
});

// Start server
app.listen(port, () => {
    console.log(`🚀 Mock Cloud License Server running on port ${port}`);
    console.log(`📋 Available endpoints:`);
    console.log(`   GET  http://localhost:${port}/api/license/health`);
    console.log(`   POST http://localhost:${port}/api/license/device-event`);
    console.log(`   POST http://localhost:${port}/api/license/validate-account`);
    console.log(`   GET  http://localhost:${port}/api/debug/devices`);
    console.log(`🔑 Bearer token: test-bearer-token-12345`);
    console.log(`📱 Max devices: ${mockData.accountInfo.maxAllowedDevices}`);
});
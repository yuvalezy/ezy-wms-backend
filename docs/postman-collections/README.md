# EzyWMS API Postman Collection

## Overview
This collection contains comprehensive API documentation and examples for the EzyWMS warehouse management system.

## Files Generated
- `EzyWMS-API-Collection.postman_collection.json` - Complete API collection with 13 endpoint groups
- `EzyWMS-API-Environment.postman_environment.json` - Environment variables for API testing

## Setup Instructions

1. **Import Collection**: Import the collection file into Postman
2. **Import Environment**: Import the environment file into Postman  
3. **Set Environment**: Select the "EzyWMS API Environment" in Postman
4. **Update Password**: Set your password in the environment variable `password`
5. **Login**: Run the "🔐 Authentication → Login" request to get your bearer token
6. **Start Testing**: All authenticated endpoints will automatically use the bearer token

## Authentication
- The collection uses Bearer token authentication
- Login via the Authentication folder to automatically set the token
- The token is automatically applied to all authenticated requests

## API Documentation
- **Base URL**: http://192.168.88.24:5000
- **Swagger UI**: http://192.168.88.24:5000/swagger
- **OpenAPI Spec**: http://192.168.88.24:5000/swagger/v1/swagger.json

## Generated on
2025-07-04T21:04:04.193Z

## Collection Statistics
- Total Endpoints: 90
- Total Folders: 13
- Authentication: JWT Bearer Token
- Content-Type: application/json

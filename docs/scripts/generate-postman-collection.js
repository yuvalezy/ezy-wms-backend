#!/usr/bin/env node

/**
 * Postman Collection Generator for EzyWMS API
 * 
 * This script generates a comprehensive Postman collection and environment
 * from the OpenAPI specification served by the running API.
 * 
 * Prerequisites:
 * - Node.js installed
 * - API server running on localhost:5000 (or update BASE_URL)
 * - npm install axios (install axios if not available)
 * 
 * Usage:
 * node generate-postman-collection.js
 */

const https = require('https');
const http = require('http');
const fs = require('fs');
const path = require('path');

// Configuration
const BASE_URL = 'http://192.168.88.24:5000';
const SWAGGER_URL = `${BASE_URL}/swagger/v1/swagger.json`;
const OUTPUT_DIR = '../postman-collections';

// Create output directory if it doesn't exist
if (!fs.existsSync(OUTPUT_DIR)) {
    fs.mkdirSync(OUTPUT_DIR, { recursive: true });
}

/**
 * Fetch data from URL
 */
function fetchData(url) {
    return new Promise((resolve, reject) => {
        const client = url.startsWith('https') ? https : http;
        
        client.get(url, (res) => {
            let data = '';
            
            res.on('data', (chunk) => {
                data += chunk;
            });
            
            res.on('end', () => {
                try {
                    resolve(JSON.parse(data));
                } catch (error) {
                    reject(new Error(`Failed to parse JSON: ${error.message}`));
                }
            });
        }).on('error', (error) => {
            reject(error);
        });
    });
}

/**
 * Convert OpenAPI path parameters to Postman format
 */
function convertPathParams(path) {
    return path.replace(/{([^}]+)}/g, ':$1');
}

/**
 * Generate request body from OpenAPI schema
 */
function generateRequestBody(requestBody) {
    if (!requestBody || !requestBody.content) return null;
    
    const jsonContent = requestBody.content['application/json'];
    if (!jsonContent || !jsonContent.schema) return null;
    
    // Generate example based on schema
    const schema = jsonContent.schema;
    
    if (schema.example) {
        return JSON.stringify(schema.example, null, 2);
    }
    
    // Generate basic example based on schema properties
    if (schema.properties) {
        const example = {};
        Object.keys(schema.properties).forEach(key => {
            const prop = schema.properties[key];
            switch (prop.type) {
                case 'string':
                    example[key] = prop.example || `sample_${key}`;
                    break;
                case 'integer':
                    example[key] = prop.example || 1;
                    break;
                case 'boolean':
                    example[key] = prop.example || false;
                    break;
                case 'array':
                    example[key] = [];
                    break;
                case 'object':
                    example[key] = {};
                    break;
                default:
                    example[key] = null;
            }
        });
        return JSON.stringify(example, null, 2);
    }
    
    return null;
}

/**
 * Generate Postman collection from OpenAPI spec
 */
function generatePostmanCollection(openApiSpec) {
    const collection = {
        info: {
            name: openApiSpec.info.title || 'EzyWMS API',
            description: openApiSpec.info.description || 'ASP.NET Core Web API for EzyWMS warehouse management system',
            version: openApiSpec.info.version || '1.0.0',
            schema: 'https://schema.getpostman.com/json/collection/v2.1.0/collection.json'
        },
        auth: {
            type: 'bearer',
            bearer: [
                {
                    key: 'token',
                    value: '{{bearerToken}}',
                    type: 'string'
                }
            ]
        },
        variable: [
            {
                key: 'baseUrl',
                value: BASE_URL,
                type: 'string'
            }
        ],
        item: []
    };
    
    // Group endpoints by controller/tag
    const controllerGroups = {};
    
    Object.keys(openApiSpec.paths).forEach(pathKey => {
        const pathItem = openApiSpec.paths[pathKey];
        
        Object.keys(pathItem).forEach(method => {
            if (!['get', 'post', 'put', 'delete', 'patch'].includes(method)) return;
            
            const operation = pathItem[method];
            const tag = operation.tags && operation.tags[0] ? operation.tags[0] : 'Default';
            
            if (!controllerGroups[tag]) {
                controllerGroups[tag] = {
                    name: tag,
                    item: []
                };
            }
            
            // Generate request
            const request = {
                name: operation.summary || `${method.toUpperCase()} ${pathKey}`,
                request: {
                    method: method.toUpperCase(),
                    header: [
                        {
                            key: 'Content-Type',
                            value: 'application/json',
                            type: 'text'
                        }
                    ],
                    url: {
                        raw: `{{baseUrl}}${convertPathParams(pathKey)}`,
                        host: ['{{baseUrl}}'],
                        path: convertPathParams(pathKey).split('/').filter(p => p)
                    },
                    description: operation.description || operation.summary || ''
                }
            };
            
            // Add request body for POST/PUT/PATCH
            if (['post', 'put', 'patch'].includes(method) && operation.requestBody) {
                const body = generateRequestBody(operation.requestBody);
                if (body) {
                    request.request.body = {
                        mode: 'raw',
                        raw: body,
                        options: {
                            raw: {
                                language: 'json'
                            }
                        }
                    };
                }
            }
            
            // Add query parameters
            if (operation.parameters) {
                const queryParams = operation.parameters.filter(p => p.in === 'query');
                if (queryParams.length > 0) {
                    request.request.url.query = queryParams.map(param => ({
                        key: param.name,
                        value: param.example || (param.schema && param.schema.example) || '',
                        description: param.description || '',
                        disabled: !param.required
                    }));
                }
            }
            
            // Add authorization requirement info
            if (operation.security && operation.security.length > 0) {
                request.request.auth = {
                    type: 'bearer',
                    bearer: [
                        {
                            key: 'token',
                            value: '{{bearerToken}}',
                            type: 'string'
                        }
                    ]
                };
            }
            
            controllerGroups[tag].item.push(request);
        });
    });
    
    // Add authentication folder first
    const authFolder = {
        name: '🔐 Authentication',
        item: [
            {
                name: 'Login',
                request: {
                    method: 'POST',
                    header: [
                        {
                            key: 'Content-Type',
                            value: 'application/json'
                        }
                    ],
                    body: {
                        mode: 'raw',
                        raw: JSON.stringify({
                            password: '{{password}}'
                        }, null, 2)
                    },
                    url: {
                        raw: '{{baseUrl}}/api/authentication/login',
                        host: ['{{baseUrl}}'],
                        path: ['api', 'authentication', 'login']
                    },
                    description: 'Login and get bearer token. The token will be automatically set in the environment.'
                },
                event: [
                    {
                        listen: 'test',
                        script: {
                            exec: [
                                'if (pm.response.code === 200) {',
                                '    const response = pm.response.json();',
                                '    if (response.token) {',
                                '        pm.environment.set(\"bearerToken\", response.token);',
                                '        console.log(\"Bearer token set successfully\");',
                                '    }',
                                '}'
                            ]
                        }
                    }
                ]
            },
            {
                name: 'Get Company Info',
                request: {
                    method: 'GET',
                    header: [],
                    url: {
                        raw: '{{baseUrl}}/api/authentication/CompanyName',
                        host: ['{{baseUrl}}'],
                        path: ['api', 'authentication', 'CompanyName']
                    },
                    description: 'Get company information (no authentication required)'
                }
            }
        ]
    };
    
    collection.item.push(authFolder);
    
    // Add controller groups in logical order
    const controllerOrder = [
        'GoodsReceipt', 'Package', 'Transfer', 'Counting', 'Picking', 
        'User', 'Authentication', 'Authorization', 'General', 'Service'
    ];
    
    controllerOrder.forEach(controllerName => {
        if (controllerGroups[controllerName]) {
            collection.item.push(controllerGroups[controllerName]);
            delete controllerGroups[controllerName];
        }
    });
    
    // Add remaining controllers
    Object.values(controllerGroups).forEach(group => {
        collection.item.push(group);
    });
    
    return collection;
}

/**
 * Generate Postman environment
 */
function generatePostmanEnvironment() {
    return {
        name: 'EzyWMS API Environment',
        values: [
            {
                key: 'baseUrl',
                value: BASE_URL,
                description: 'Base URL for the EzyWMS API',
                enabled: true
            },
            {
                key: 'bearerToken',
                value: '',
                description: 'JWT Bearer token for authentication (set automatically after login)',
                enabled: true
            },
            {
                key: 'password',
                value: 'your_password_here',
                description: 'Password for login (update with your actual password)',
                enabled: true
            }
        ],
        _postman_variable_scope: 'environment'
    };
}

/**
 * Main function
 */
async function main() {
    try {
        console.log('🚀 Generating Postman collection for EzyWMS API...');
        console.log(`📡 Fetching OpenAPI spec from: ${SWAGGER_URL}`);
        
        const openApiSpec = await fetchData(SWAGGER_URL);
        
        console.log(`✅ OpenAPI spec loaded successfully`);
        console.log(`📊 Found ${Object.keys(openApiSpec.paths).length} endpoints`);
        
        // Generate collection
        const collection = generatePostmanCollection(openApiSpec);
        const collectionPath = path.join(OUTPUT_DIR, 'EzyWMS-API-Collection.postman_collection.json');
        fs.writeFileSync(collectionPath, JSON.stringify(collection, null, 2));
        
        console.log(`📁 Postman collection saved to: ${collectionPath}`);
        console.log(`📋 Collection contains ${collection.item.length} folders`);
        
        // Generate environment
        const environment = generatePostmanEnvironment();
        const environmentPath = path.join(OUTPUT_DIR, 'EzyWMS-API-Environment.postman_environment.json');
        fs.writeFileSync(environmentPath, JSON.stringify(environment, null, 2));
        
        console.log(`🌍 Postman environment saved to: ${environmentPath}`);
        
        // Generate README
        const readmePath = path.join(OUTPUT_DIR, 'README.md');
        const readmeContent = `# EzyWMS API Postman Collection

## Overview
This collection contains comprehensive API documentation and examples for the EzyWMS warehouse management system.

## Files Generated
- \`EzyWMS-API-Collection.postman_collection.json\` - Complete API collection with ${collection.item.length} endpoint groups
- \`EzyWMS-API-Environment.postman_environment.json\` - Environment variables for API testing

## Setup Instructions

1. **Import Collection**: Import the collection file into Postman
2. **Import Environment**: Import the environment file into Postman  
3. **Set Environment**: Select the "EzyWMS API Environment" in Postman
4. **Update Password**: Set your password in the environment variable \`password\`
5. **Login**: Run the "🔐 Authentication → Login" request to get your bearer token
6. **Start Testing**: All authenticated endpoints will automatically use the bearer token

## Authentication
- The collection uses Bearer token authentication
- Login via the Authentication folder to automatically set the token
- The token is automatically applied to all authenticated requests

## API Documentation
- **Base URL**: ${BASE_URL}
- **Swagger UI**: ${BASE_URL}/swagger
- **OpenAPI Spec**: ${SWAGGER_URL}

## Generated on
${new Date().toISOString()}

## Collection Statistics
- Total Endpoints: ${Object.keys(openApiSpec.paths).length}
- Total Folders: ${collection.item.length}
- Authentication: JWT Bearer Token
- Content-Type: application/json
`;
        
        fs.writeFileSync(readmePath, readmeContent);
        console.log(`📖 README saved to: ${readmePath}`);
        
        console.log('\\n🎉 Postman collection generation completed successfully!');
        console.log('\\n📋 Next steps:');
        console.log('1. Import the collection and environment files into Postman');
        console.log('2. Update the password in the environment variables');
        console.log('3. Run the Login request to authenticate');
        console.log('4. Start testing the API endpoints!');
        
    } catch (error) {
        console.error('❌ Error generating Postman collection:', error.message);
        
        if (error.code === 'ECONNREFUSED') {
            console.error('\\n💡 Make sure the API server is running on', BASE_URL);
            console.error('   You can start it with: dotnet run --project Service');
        }
        
        process.exit(1);
    }
}

// Run the generator
if (require.main === module) {
    main();
}

module.exports = { generatePostmanCollection, generatePostmanEnvironment };
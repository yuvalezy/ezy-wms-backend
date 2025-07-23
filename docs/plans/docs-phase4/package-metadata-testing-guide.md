# Package Metadata Testing Guide

## Overview

This guide provides comprehensive testing procedures for the Package Metadata feature, including unit tests, integration tests, and end-to-end validation scenarios.

## Test Categories

### 1. Configuration Validation Tests

#### Backend Unit Tests (Already Implemented)

Location: `lw/UnitTests/Unit/Settings/PackageMetadataTests.cs`

**Test Coverage:**
- ✅ Valid configuration validation
- ✅ Duplicate ID detection  
- ✅ Invalid identifier validation
- ✅ Empty description detection
- ✅ Case sensitivity handling
- ✅ Special character validation

**Run Tests:**
```bash
cd lw
dotnet test UnitTests --filter "PackageMetadataTests"
```

#### Configuration Test Scenarios

**Scenario 1: Valid Configuration**
```json
{
  "Package": {
    "MetadataDefinition": [
      {"Type": "String", "Id": "Note", "Description": "Notes"},
      {"Type": "Decimal", "Id": "Volume", "Description": "Volume (m³)"},
      {"Type": "Date", "Id": "ExpiryDate", "Description": "Expiry Date"}
    ]
  }
}
```
Expected: ✅ Configuration loads successfully

**Scenario 2: Duplicate IDs**
```json
{
  "Package": {
    "MetadataDefinition": [
      {"Type": "String", "Id": "Note", "Description": "Note 1"},
      {"Type": "String", "Id": "note", "Description": "Note 2"}
    ]
  }
}
```
Expected: ❌ Validation error: "Duplicate metadata definition ID: Note"

**Scenario 3: Invalid Identifiers**
```json
{
  "Package": {
    "MetadataDefinition": [
      {"Type": "String", "Id": "2Volume", "Description": "Volume"},
      {"Type": "String", "Id": "Weight KG", "Description": "Weight"}
    ]
  }
}
```
Expected: ❌ Validation errors for invalid identifiers

### 2. API Integration Tests

#### Backend Service Tests (Already Implemented)

Location: `lw/UnitTests/Unit/Services/PackageServiceMetadataTests.cs`

**Test Coverage:**
- ✅ DTO creation and validation
- ✅ Form state management
- ✅ Data type handling

**Run Tests:**
```bash
cd lw
dotnet test UnitTests --filter "PackageServiceMetadataTests"
```

#### API Endpoint Tests

**Test Setup:**
1. Configure test metadata definitions
2. Create test package
3. Test CRUD operations

**GET /api/general/package-metadata-definitions**
```bash
curl -X GET "https://localhost:5001/api/general/package-metadata-definitions" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

Expected Response:
```json
[
  {
    "id": "Volume",
    "description": "Volume (m³)", 
    "type": 1
  }
]
```

**PUT /api/package/{id}/metadata**
```bash
curl -X PUT "https://localhost:5001/api/package/123e4567-e89b-12d3-a456-426614174000/metadata" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "metadata": {
      "Volume": 10.5,
      "Note": "Test package",
      "ExpiryDate": "2025-12-31"
    }
  }'
```

Expected Response: Updated PackageDto with metadata

### 3. Frontend Component Tests

#### Unit Tests (Already Implemented)

Location: `lw_fe/src/features/packages/components/PackageMetadataDisplay.test.tsx`

**Test Coverage:**
- ✅ Component rendering with/without data
- ✅ Field type badge display
- ✅ Null value handling
- ✅ Internationalization support

**Run Tests:**
```bash
cd lw_fe
npm test -- --testPathPattern=PackageMetadataDisplay.test.tsx
```

#### Additional Frontend Test Scenarios

**Test 1: Form Validation**
1. Open PackageMetadataForm with invalid data
2. Verify validation messages appear
3. Verify save button is disabled
4. Correct data and verify save enables

**Test 2: Dynamic Form Generation** 
1. Configure different field types
2. Verify correct input types render
3. Test form submission with various data types

**Test 3: Integration with Package Check**
1. Navigate to package check page
2. Scan package with metadata
3. Verify metadata displays correctly
4. Test metadata editing workflow

### 4. End-to-End Test Scenarios

#### Scenario 1: Food Industry Package

**Setup Configuration:**
```json
{
  "Package": {
    "MetadataDefinition": [
      {"Type": "Date", "Id": "ExpiryDate", "Description": "Expiration Date"},
      {"Type": "String", "Id": "BatchNumber", "Description": "Batch Number"},
      {"Type": "Decimal", "Id": "Temperature", "Description": "Storage Temperature (°C)"}
    ]
  }
}
```

**Test Steps:**
1. Create new package via API or UI
2. Add metadata: ExpiryDate=2025-12-31, BatchNumber=BATCH001, Temperature=-18.5
3. Verify package shows metadata in check view
4. Update metadata via form
5. Verify changes persist
6. Export package data to Excel (if applicable)
7. Verify metadata included in export

**Expected Results:**
- ✅ Metadata saves successfully
- ✅ Displays in package check view
- ✅ Form allows editing
- ✅ Changes persist after refresh
- ✅ Data types validated correctly

#### Scenario 2: Pharmaceutical Package

**Setup Configuration:**
```json
{
  "Package": {
    "MetadataDefinition": [
      {"Type": "String", "Id": "LotNumber", "Description": "Lot Number"},
      {"Type": "Date", "Id": "ExpiryDate", "Description": "Expiration Date"},
      {"Type": "Decimal", "Id": "Potency", "Description": "Potency (%)"},
      {"Type": "String", "Id": "NDCNumber", "Description": "NDC Number"}
    ]
  }
}
```

**Test Steps:**
1. Create package with pharmaceutical metadata
2. Test all field types work correctly
3. Test form validation (e.g., invalid date, non-numeric potency)
4. Test null value handling (remove fields)
5. Test field persistence across sessions

#### Scenario 3: Error Handling

**Test Invalid API Requests:**
1. Send metadata with unknown field IDs
2. Send wrong data types (string for decimal field)
3. Send malformed JSON
4. Test with invalid package ID
5. Test without proper authentication

**Expected Error Responses:**
- 400 Bad Request for validation errors
- 404 Not Found for invalid package ID
- 401 Unauthorized without proper auth
- Clear error messages for client handling

#### Scenario 4: Performance Testing

**Large Metadata Sets:**
1. Configure 15+ metadata fields
2. Create packages with maximum metadata
3. Test form rendering performance
4. Test API response times
5. Test JSON serialization limits

**Bulk Operations:**
1. Create 100+ packages with metadata
2. Test batch operations if available
3. Monitor memory usage
4. Test search/filter performance

### 5. Database Integration Tests

#### Test Data Persistence

**Test 1: JSON Storage**
```sql
-- Verify metadata stored correctly in CustomAttributes column
SELECT Id, Barcode, CustomAttributes 
FROM Packages 
WHERE CustomAttributes IS NOT NULL
```

**Test 2: Data Integrity**
1. Insert package with metadata via API
2. Query database directly to verify JSON structure
3. Update metadata and verify changes
4. Delete fields (set to null) and verify removal

**Test 3: Migration Compatibility**
1. Create packages without metadata (existing behavior)
2. Add metadata configuration
3. Verify existing packages still work
4. Add metadata to existing packages
5. Remove metadata configuration
6. Verify packages still function (data preserved but not displayed)

### 6. Multilingual Testing

#### Translation Coverage

**Test Languages:**
- English (en)
- Spanish (es)

**Test Components:**
1. Field type labels (Text, Number, Date)
2. Form buttons (Save, Reset, Cancel)
3. Validation messages
4. Empty state messages
5. Error messages

**Test Procedure:**
1. Switch language in application
2. Navigate to package metadata features
3. Verify all text translates correctly
4. Test form validation messages in both languages
5. Verify field descriptions display correctly

### 7. Security Testing

#### Authentication & Authorization

**Test 1: Endpoint Access Control**
1. Test metadata endpoints without authentication
2. Verify 401 Unauthorized response
3. Test with invalid JWT token
4. Test with expired token

**Test 2: Package Access Control**
1. User from Warehouse A tries to access Package from Warehouse B
2. Verify 404 Not Found (not 403 to avoid info leakage)
3. Test with proper warehouse permissions

**Test 3: Input Validation Security**
1. Test XSS prevention in string fields
2. Test SQL injection prevention  
3. Test JSON injection attacks
4. Test extremely large payloads
5. Test special character handling

#### Data Sanitization

**Test Input Sanitization:**
```javascript
// Test cases for string fields
const testInputs = [
  "<script>alert('xss')</script>",
  "'; DROP TABLE Packages; --",
  "\u0000\u0001\u0002", // Control characters
  "A".repeat(10000), // Very long string
  "emoji🧪test", // Unicode
  "\n\r\t", // Whitespace
];
```

### 8. Browser Compatibility Testing

#### Supported Browsers
- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

#### Test Matrix
| Feature | Chrome | Firefox | Safari | Edge |
|---------|---------|---------|---------|---------|
| Form rendering | ✅ | ✅ | ✅ | ✅ |
| Date picker | ✅ | ✅ | ✅ | ✅ |
| Number input | ✅ | ✅ | ✅ | ✅ |
| Validation | ✅ | ✅ | ✅ | ✅ |
| API calls | ✅ | ✅ | ✅ | ✅ |

### 9. Automated Test Scripts

#### Jest Test Runner (Frontend)
```bash
# Run all package metadata tests
npm test -- --testPathPattern=packages.*metadata

# Run with coverage
npm test -- --coverage --testPathPattern=packages.*metadata

# Run in watch mode during development
npm test -- --watch --testPathPattern=packages.*metadata
```

#### NUnit Test Runner (Backend)
```bash
# Run all metadata tests
dotnet test --filter "TestCategory=Metadata"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed" --filter "PackageMetadata"

# Run performance tests
dotnet test --filter "TestCategory=Performance" --logger trx
```

#### Integration Test Scripts
```bash
# Backend integration tests
cd lw
dotnet test UnitTests --filter "TestCategory=Integration"

# Full system test
./scripts/run-integration-tests.sh
```

## Test Data Setup

### Sample Configuration Files

**development.json:**
```json
{
  "Package": {
    "MetadataDefinition": [
      {"Type": "String", "Id": "TestNote", "Description": "Test Note"},
      {"Type": "Decimal", "Id": "TestVolume", "Description": "Test Volume"},
      {"Type": "Date", "Id": "TestDate", "Description": "Test Date"}
    ]
  }
}
```

**testing.json:**
```json
{
  "Package": {
    "MetadataDefinition": [
      {"Type": "String", "Id": "BatchNumber", "Description": "Batch Number"},
      {"Type": "Date", "Id": "ExpiryDate", "Description": "Expiration Date"},
      {"Type": "Decimal", "Id": "Temperature", "Description": "Temperature (°C)"},
      {"Type": "String", "Id": "Quality", "Description": "Quality Grade"},
      {"Type": "Date", "Id": "TestDate", "Description": "Quality Test Date"}
    ]
  }
}
```

### Test Data Generation

**Sample Packages with Metadata:**
```csharp
public static class TestPackages
{
    public static PackageDto CreateFoodPackage()
    {
        return new PackageDto
        {
            Id = Guid.NewGuid(),
            Barcode = "FOOD001",
            CustomAttributes = new Dictionary<string, object>
            {
                { "ExpiryDate", DateTime.Now.AddDays(30) },
                { "BatchNumber", "BATCH2025001" },
                { "Temperature", -18.5m }
            },
            MetadataDefinitions = new[]
            {
                new PackageMetadataDefinition { Id = "ExpiryDate", Description = "Expiration Date", Type = MetadataFieldType.Date },
                new PackageMetadataDefinition { Id = "BatchNumber", Description = "Batch Number", Type = MetadataFieldType.String },
                new PackageMetadataDefinition { Id = "Temperature", Description = "Temperature (°C)", Type = MetadataFieldType.Decimal }
            }
        };
    }
}
```

## Acceptance Criteria Checklist

### Backend Functionality
- [ ] ✅ Configuration validation prevents duplicate IDs
- [ ] ✅ Configuration validation prevents invalid identifiers
- [ ] ✅ API returns metadata definitions correctly
- [ ] ✅ API updates package metadata with proper validation
- [ ] ✅ API handles null values correctly (field removal)
- [ ] ✅ Warehouse access control works properly
- [ ] ✅ Error responses are user-friendly
- [ ] ✅ All field types (String, Decimal, Date) work correctly

### Frontend Functionality  
- [ ] ✅ Metadata displays in package check view
- [ ] ✅ Form generates dynamically based on configuration
- [ ] ✅ Form validation works for all field types
- [ ] ✅ Save/reset/cancel operations work correctly
- [ ] ✅ Real-time validation provides immediate feedback
- [ ] ✅ Internationalization works for both languages
- [ ] ✅ Responsive design works on different screen sizes
- [ ] ✅ Loading states provide good UX

### Integration
- [ ] ✅ Backend and frontend communicate correctly
- [ ] ✅ Error handling works end-to-end
- [ ] ✅ Authentication and authorization work properly
- [ ] ✅ Data persists correctly in database
- [ ] ✅ Performance is acceptable with large datasets

### User Experience
- [ ] ✅ Interface is intuitive and user-friendly
- [ ] ✅ Error messages are clear and actionable
- [ ] ✅ Form validation prevents user mistakes
- [ ] ✅ Loading indicators show progress
- [ ] ✅ Success feedback confirms actions

## Conclusion

This testing guide ensures comprehensive coverage of the Package Metadata feature. Follow the test scenarios systematically to validate all functionality before production deployment.
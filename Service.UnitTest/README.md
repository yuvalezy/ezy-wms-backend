# Service.UnitTest

This project contains unit and integration tests for the LW Server application.

## Running Tests

### Using dotnet CLI
```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test category
dotnet test --filter Category=Integration
dotnet test --filter Category=Unit

# Run specific test class
dotnet test --filter FullyQualifiedName~UserServiceIntegrationTests
```

### Using Visual Studio
1. Open Test Explorer (Test > Test Explorer)
2. Click "Run All Tests" or select specific tests to run
3. View test results and output in the Test Explorer window

## Test Structure

### Integration Tests
- Located in `Integration/` folder
- Use Entity Framework In-Memory database for testing
- Test full service layer with database interactions
- Base class: `IntegrationTestBase`

### Unit Tests
- Test individual components in isolation
- Use mocking frameworks (Moq) for dependencies
- Focus on business logic and edge cases

## Test Categories

Tests are organized into the following categories:
- **Integration**: Tests that interact with the database
- **Unit**: Isolated unit tests with mocked dependencies
- **Smoke**: Quick tests to verify basic functionality

## Writing New Tests

### Integration Test Example
```csharp
[TestFixture]
public class MyServiceIntegrationTests : IntegrationTestBase {
    private MyService _service;

    [SetUp]
    public override async Task BaseSetUp() {
        await base.BaseSetUp();
        _service = new MyService(DbContext);
    }

    protected override async Task SeedDataAsync() {
        // Add test data
        DbContext.MyEntities.Add(new MyEntity { /* ... */ });
        await DbContext.SaveChangesAsync();
    }

    [Test]
    public async Task MyMethod_ShouldDoSomething() {
        // Arrange
        // Act
        var result = await _service.MyMethodAsync();
        // Assert
        result.Should().NotBeNull();
    }
}
```

### Unit Test Example
```csharp
[TestFixture]
public class MyServiceUnitTests {
    private Mock<IDbContext> _dbContextMock;
    private MyService _service;

    [SetUp]
    public void SetUp() {
        _dbContextMock = new Mock<IDbContext>();
        _service = new MyService(_dbContextMock.Object);
    }

    [Test]
    public void MyMethod_ShouldReturnExpectedResult() {
        // Arrange
        _dbContextMock.Setup(x => x.GetData()).Returns(testData);
        // Act
        var result = _service.ProcessData();
        // Assert
        result.Should().Be(expected);
    }
}
```

## Test Data Helpers

Use `TestDatabaseHelper` to create in-memory database contexts:
```csharp
var context = TestDatabaseHelper.CreateInMemoryContext();
```

## Best Practices

1. **Isolation**: Each test should be independent and not rely on other tests
2. **Naming**: Use descriptive test names following the pattern: `MethodName_Scenario_ExpectedResult`
3. **AAA Pattern**: Follow Arrange-Act-Assert pattern
4. **Clean Up**: Always dispose of resources in TearDown methods
5. **Test Data**: Use realistic test data that covers edge cases
6. **Assertions**: Use FluentAssertions for readable test assertions

## Dependencies

- **NUnit**: Testing framework
- **Moq**: Mocking framework
- **FluentAssertions**: Assertion library
- **Microsoft.EntityFrameworkCore.InMemory**: In-memory database for integration tests
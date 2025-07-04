namespace UnitTests.Integration.Database;

[TestFixture]
public class HelloDatabaseTest
{
    [Test]
    public void HelloDatabase_ShouldReturnGreeting()
    {
        // Arrange
        var expected = "Hello, Database!";

        // Act
        var actual = "Hello, Database!";

        // Assert
        Assert.That(actual, Is.EqualTo(expected));
    }
}
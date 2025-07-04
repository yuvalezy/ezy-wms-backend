namespace UnitTests.Unit.Repositories;

[TestFixture]
public class HelloRepositoryTest
{
    [Test]
    public void HelloRepository_ShouldReturnGreeting()
    {
        // Arrange
        var expected = "Hello, Repository!";

        // Act
        var actual = "Hello, Repository!";

        // Assert
        Assert.That(actual, Is.EqualTo(expected));
    }
}
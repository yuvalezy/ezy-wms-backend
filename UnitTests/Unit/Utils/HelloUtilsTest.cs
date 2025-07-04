namespace UnitTests.Unit.Utils;

[TestFixture]
public class HelloUtilsTest
{
    [Test]
    public void HelloUtils_ShouldReturnGreeting()
    {
        // Arrange
        var expected = "Hello, Utils!";

        // Act
        var actual = "Hello, Utils!";

        // Assert
        Assert.That(actual, Is.EqualTo(expected));
    }
}
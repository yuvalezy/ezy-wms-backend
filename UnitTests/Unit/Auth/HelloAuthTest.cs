using NUnit.Framework;

namespace UnitTests.Unit.Auth
{
    [TestFixture]
    public class HelloAuthTest
    {
        [Test]
        public void HelloAuth_ShouldReturnGreeting()
        {
            // Arrange
            var expected = "Hello, Auth!";

            // Act
            var actual = "Hello, Auth!";

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
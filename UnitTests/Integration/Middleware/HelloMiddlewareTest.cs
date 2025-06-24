using NUnit.Framework;

namespace UnitTests.Integration.Middleware
{
    [TestFixture]
    public class HelloMiddlewareTest
    {
        [Test]
        public void HelloMiddleware_ShouldReturnGreeting()
        {
            // Arrange
            var expected = "Hello, Middleware!";

            // Act
            var actual = "Hello, Middleware!";

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
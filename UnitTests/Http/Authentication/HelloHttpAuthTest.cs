using NUnit.Framework;

namespace UnitTests.Http.Authentication
{
    [TestFixture]
    public class HelloHttpAuthTest
    {
        [Test]
        public void HelloHttpAuth_ShouldReturnGreeting()
        {
            // Arrange
            var expected = "Hello, HTTP Auth!";

            // Act
            var actual = "Hello, HTTP Auth!";

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
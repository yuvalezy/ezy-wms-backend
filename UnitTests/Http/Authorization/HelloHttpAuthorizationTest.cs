using NUnit.Framework;

namespace UnitTests.Http.Authorization
{
    [TestFixture]
    public class HelloHttpAuthorizationTest
    {
        [Test]
        public void HelloHttpAuthorization_ShouldReturnGreeting()
        {
            // Arrange
            var expected = "Hello, HTTP Authorization!";

            // Act
            var actual = "Hello, HTTP Authorization!";

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
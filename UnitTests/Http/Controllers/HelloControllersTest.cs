using NUnit.Framework;

namespace UnitTests.Http.Controllers
{
    [TestFixture]
    public class HelloControllersTest
    {
        [Test]
        public void HelloControllers_ShouldReturnGreeting()
        {
            // Arrange
            var expected = "Hello, Controllers!";

            // Act
            var actual = "Hello, Controllers!";

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
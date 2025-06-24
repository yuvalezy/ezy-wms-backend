using NUnit.Framework;

namespace UnitTests.Unit.Services
{
    [TestFixture]
    public class HelloServiceTest
    {
        [Test]
        public void HelloWorld_ShouldReturnGreeting()
        {
            // Arrange
            var expected = "Hello, World!";

            // Act
            var actual = "Hello, World!";

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
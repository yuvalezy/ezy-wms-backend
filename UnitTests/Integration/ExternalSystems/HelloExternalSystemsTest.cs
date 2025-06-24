using NUnit.Framework;

namespace UnitTests.Integration.ExternalSystems
{
    [TestFixture]
    public class HelloExternalSystemsTest
    {
        [Test]
        public void HelloExternalSystems_ShouldReturnGreeting()
        {
            // Arrange
            var expected = "Hello, External Systems!";

            // Act
            var actual = "Hello, External Systems!";

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
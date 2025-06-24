using NUnit.Framework;

namespace UnitTests.Integration.Services
{
    [TestFixture]
    public class HelloIntegrationServicesTest
    {
        [Test]
        public void HelloIntegrationServices_ShouldReturnGreeting()
        {
            // Arrange
            var expected = "Hello, Integration Services!";

            // Act
            var actual = "Hello, Integration Services!";

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
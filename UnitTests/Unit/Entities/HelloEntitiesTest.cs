using NUnit.Framework;

namespace UnitTests.Unit.Entities
{
    [TestFixture]
    public class HelloEntitiesTest
    {
        [Test]
        public void HelloEntities_ShouldReturnGreeting()
        {
            // Arrange
            var expected = "Hello, Entities!";

            // Act
            var actual = "Hello, Entities!";

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
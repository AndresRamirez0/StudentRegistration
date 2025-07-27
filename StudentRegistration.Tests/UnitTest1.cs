using Xunit;
using FluentAssertions;

namespace StudentRegistration.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            // Arrange
            var expected = 4;
            
            // Act
            var actual = 2 + 2;
            
            // Assert
            actual.Should().Be(expected);
        }

        [Fact]
        public void SimpleAssertionTest()
        {
            // Arrange & Act
            var result = "Hello World";
            
            // Assert
            result.Should().NotBeEmpty();
            result.Should().Contain("World");
        }
    }
}
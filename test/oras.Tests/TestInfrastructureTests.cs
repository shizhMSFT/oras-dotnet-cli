using FluentAssertions;
using Xunit;

namespace Oras.Tests;

/// <summary>
/// Basic smoke tests for the test infrastructure itself.
/// </summary>
public class TestInfrastructureTests
{
    [Fact]
    public void FluentAssertions_IsAvailable()
    {
        // Arrange
        var value = 42;

        // Act & Assert
        value.Should().Be(42);
    }

    [Fact]
    public void BasicTest_Passes()
    {
        // Arrange
        var result = 2 + 2;

        // Assert
        Assert.Equal(4, result);
    }

    [Fact]
    public void AsyncTest_Completes()
    {
        // Arrange & Act
        var task = Task.CompletedTask;

        // Assert
        task.IsCompleted.Should().BeTrue();
    }
}

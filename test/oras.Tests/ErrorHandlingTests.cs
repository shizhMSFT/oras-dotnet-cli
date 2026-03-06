using FluentAssertions;
using Xunit;

namespace Oras.Tests;

/// <summary>
/// Unit tests for error handling and exception types.
/// </summary>
public sealed class ErrorHandlingTests
{
    [Fact]
    public void OrasException_WithMessage_CreatesException()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new OrasException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.Recommendation.Should().BeNull();
    }

    [Fact]
    public void OrasException_WithMessageAndRecommendation_CreatesException()
    {
        // Arrange
        var message = "Test error message";
        var recommendation = "Try this fix";

        // Act
        var exception = new OrasException(message, recommendation);

        // Assert
        exception.Message.Should().Be(message);
        exception.Recommendation.Should().Be(recommendation);
    }

    [Fact]
    public void OrasUsageException_WithMessage_CreatesException()
    {
        // Arrange
        var message = "Invalid argument";

        // Act
        var exception = new OrasUsageException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.Recommendation.Should().BeNull();
    }

    [Fact]
    public void OrasUsageException_InheritsFromOrasException()
    {
        // Arrange & Act
        var exception = new OrasUsageException("test");

        // Assert
        exception.Should().BeAssignableTo<OrasException>();
    }

    [Fact]
    public void OrasAuthenticationException_WithMessage_CreatesException()
    {
        // Arrange
        var message = "Authentication failed";

        // Act
        var exception = new OrasAuthenticationException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void OrasAuthenticationException_InheritsFromOrasException()
    {
        // Arrange & Act
        var exception = new OrasAuthenticationException("test");

        // Assert
        exception.Should().BeAssignableTo<OrasException>();
    }
}

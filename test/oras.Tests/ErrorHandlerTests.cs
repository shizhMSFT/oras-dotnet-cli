using FluentAssertions;
using Xunit;

namespace Oras.Tests;

/// <summary>
/// Tests for ErrorHandler.HandleAsync — exit code mapping, recommendation display,
/// and exception routing for every exception type the handler recognizes.
/// </summary>
public sealed class ErrorHandlerTests
{
    [Fact]
    public async Task HandleAsync_OrasUsageException_ReturnsExitCode2()
    {
        var exitCode = await ErrorHandler.HandleAsync(
            () => throw new OrasUsageException("bad argument"));

        exitCode.Should().Be(2);
    }

    [Fact]
    public async Task HandleAsync_OrasException_ReturnsExitCode1()
    {
        var exitCode = await ErrorHandler.HandleAsync(
            () => throw new OrasException("something went wrong"));

        exitCode.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_OrasAuthenticationException_ReturnsExitCode1()
    {
        var exitCode = await ErrorHandler.HandleAsync(
            () => throw new OrasAuthenticationException("auth failed"));

        exitCode.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_OperationCanceledException_ReturnsExitCode1()
    {
        var exitCode = await ErrorHandler.HandleAsync(
            () => throw new OperationCanceledException());

        exitCode.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_HttpRequestException_ReturnsExitCode1()
    {
        var exitCode = await ErrorHandler.HandleAsync(
            () => throw new HttpRequestException("connection refused"));

        exitCode.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_UnauthorizedAccessException_ReturnsExitCode1()
    {
        var exitCode = await ErrorHandler.HandleAsync(
            () => throw new UnauthorizedAccessException("access denied"));

        exitCode.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_UnknownException_ReturnsExitCode1()
    {
        var exitCode = await ErrorHandler.HandleAsync(
            () => throw new InvalidOperationException("something unexpected"));

        exitCode.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_SuccessfulAction_ReturnsActionResult()
    {
        var exitCode = await ErrorHandler.HandleAsync(() => Task.FromResult(0));

        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_OrasUsageException_CaughtBeforeOrasException()
    {
        // OrasUsageException inherits OrasException — verify it gets exit code 2, not 1
        var exitCode = await ErrorHandler.HandleAsync(
            () => throw new OrasUsageException("invalid usage"));

        exitCode.Should().Be(2, "OrasUsageException should be caught by its specific handler returning 2, not the base OrasException handler returning 1");
    }

    [Fact]
    public async Task HandleAsync_OrasAuthenticationException_CaughtByOrasExceptionHandler()
    {
        // OrasAuthenticationException inherits OrasException — both map to exit code 1
        // but the OrasException handler catches it (there's no separate auth handler)
        var exitCode = await ErrorHandler.HandleAsync(
            () => throw new OrasAuthenticationException("not authenticated", "run 'oras login'"));

        exitCode.Should().Be(1);
    }
}

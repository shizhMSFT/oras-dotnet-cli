using FluentAssertions;
using Oras.Tests.Helpers;
using Xunit;

namespace Oras.Tests.Helpers;

/// <summary>
/// Tests for TestCredentialStore helper.
/// </summary>
public class TestCredentialStoreTests
{
    [Fact]
    public void Store_AddsCredential()
    {
        // Arrange
        var store = new TestCredentialStore();

        // Act
        store.Store("registry.example.com", "user1", "pass1");

        // Assert
        store.Contains("registry.example.com").Should().BeTrue();
        store.Count.Should().Be(1);
    }

    [Fact]
    public void Get_ReturnsStoredCredential()
    {
        // Arrange
        var store = new TestCredentialStore();
        store.Store("registry.example.com", "user1", "pass1");

        // Act
        var credential = store.Get("registry.example.com");

        // Assert
        credential.Should().NotBeNull();
        credential!.Username.Should().Be("user1");
        credential.Password.Should().Be("pass1");
    }

    [Fact]
    public void Get_ReturnsNull_WhenCredentialDoesNotExist()
    {
        // Arrange
        var store = new TestCredentialStore();

        // Act
        var credential = store.Get("nonexistent.example.com");

        // Assert
        credential.Should().BeNull();
    }

    [Fact]
    public void Remove_RemovesCredential()
    {
        // Arrange
        var store = new TestCredentialStore();
        store.Store("registry.example.com", "user1", "pass1");

        // Act
        var removed = store.Remove("registry.example.com");

        // Assert
        removed.Should().BeTrue();
        store.Contains("registry.example.com").Should().BeFalse();
        store.Count.Should().Be(0);
    }

    [Fact]
    public void Remove_ReturnsFalse_WhenCredentialDoesNotExist()
    {
        // Arrange
        var store = new TestCredentialStore();

        // Act
        var removed = store.Remove("nonexistent.example.com");

        // Assert
        removed.Should().BeFalse();
    }

    [Fact]
    public void Clear_RemovesAllCredentials()
    {
        // Arrange
        var store = new TestCredentialStore();
        store.Store("registry1.example.com", "user1", "pass1");
        store.Store("registry2.example.com", "user2", "pass2");

        // Act
        store.Clear();

        // Assert
        store.Count.Should().Be(0);
        store.Contains("registry1.example.com").Should().BeFalse();
        store.Contains("registry2.example.com").Should().BeFalse();
    }

    [Fact]
    public void Store_ThrowsArgumentException_WhenRegistryIsNull()
    {
        // Arrange
        var store = new TestCredentialStore();

        // Act & Assert
        var act = () => store.Store(null!, "user", "pass");
        act.Should().Throw<ArgumentException>().WithParameterName("registry");
    }

    [Fact]
    public void Store_ThrowsArgumentException_WhenRegistryIsEmpty()
    {
        // Arrange
        var store = new TestCredentialStore();

        // Act & Assert
        var act = () => store.Store(string.Empty, "user", "pass");
        act.Should().Throw<ArgumentException>().WithParameterName("registry");
    }

    [Fact]
    public void Get_ThrowsArgumentException_WhenRegistryIsNull()
    {
        // Arrange
        var store = new TestCredentialStore();

        // Act & Assert
        var act = () => store.Get(null!);
        act.Should().Throw<ArgumentException>().WithParameterName("registry");
    }

    [Fact]
    public void Remove_ThrowsArgumentException_WhenRegistryIsNull()
    {
        // Arrange
        var store = new TestCredentialStore();

        // Act & Assert
        var act = () => store.Remove(null!);
        act.Should().Throw<ArgumentException>().WithParameterName("registry");
    }

    [Fact]
    public void Store_OverwritesExistingCredential()
    {
        // Arrange
        var store = new TestCredentialStore();
        store.Store("registry.example.com", "user1", "pass1");

        // Act
        store.Store("registry.example.com", "user2", "pass2");

        // Assert
        var credential = store.Get("registry.example.com");
        credential.Should().NotBeNull();
        credential!.Username.Should().Be("user2");
        credential.Password.Should().Be("pass2");
        store.Count.Should().Be(1);
    }
}

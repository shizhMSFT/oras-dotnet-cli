using FluentAssertions;
using Oras.Options;
using System.CommandLine;
using Xunit;

namespace Oras.Tests.Options;

/// <summary>
/// Unit tests for option classes.
/// </summary>
public sealed class OptionParsingTests
{
    [Fact]
    public void RemoteOptions_HasUsernameOption()
    {
        // Arrange & Act
        var options = new RemoteOptions();

        // Assert
        options.UsernameOption.Should().NotBeNull();
        options.UsernameOption.Name.Should().Be("--username");
        options.UsernameOption.Aliases.Should().Contain("-u");
    }

    [Fact]
    public void RemoteOptions_HasPasswordOption()
    {
        // Arrange & Act
        var options = new RemoteOptions();

        // Assert
        options.PasswordOption.Should().NotBeNull();
        options.PasswordOption.Name.Should().Be("--password");
        options.PasswordOption.Aliases.Should().Contain("-p");
    }

    [Fact]
    public void RemoteOptions_HasPasswordStdinOption()
    {
        // Arrange & Act
        var options = new RemoteOptions();

        // Assert
        options.PasswordStdinOption.Should().NotBeNull();
        options.PasswordStdinOption.Name.Should().Be("--password-stdin");
    }

    [Fact]
    public void RemoteOptions_HasPlainHttpOption()
    {
        // Arrange & Act
        var options = new RemoteOptions();

        // Assert
        options.PlainHttpOption.Should().NotBeNull();
        options.PlainHttpOption.Name.Should().Be("--plain-http");
    }

    [Fact]
    public void RemoteOptions_HasInsecureOption()
    {
        // Arrange & Act
        var options = new RemoteOptions();

        // Assert
        options.InsecureOption.Should().NotBeNull();
        options.InsecureOption.Name.Should().Be("--insecure");
    }

    [Fact]
    public void RemoteOptions_ApplyToCommand_AddsAllOptions()
    {
        // Arrange
        var options = new RemoteOptions();
        var command = new Command("test");

        // Act
        options.ApplyTo(command);

        // Assert
        command.Options.Should().HaveCountGreaterOrEqualTo(5, "should add at least 5 options");
        command.Options.Should().Contain(o => o.Name == "--username");
        command.Options.Should().Contain(o => o.Name == "--password");
        command.Options.Should().Contain(o => o.Name == "--password-stdin");
        command.Options.Should().Contain(o => o.Name == "--plain-http");
        command.Options.Should().Contain(o => o.Name == "--insecure");
    }

    [Fact]
    public void PackerOptions_HasArtifactTypeOption()
    {
        // Arrange & Act
        var options = new PackerOptions();

        // Assert
        options.ArtifactTypeOption.Should().NotBeNull();
        options.ArtifactTypeOption.Name.Should().Be("--artifact-type");
    }

    [Fact]
    public void PackerOptions_HasAnnotationOption()
    {
        // Arrange & Act
        var options = new PackerOptions();

        // Assert
        options.AnnotationOption.Should().NotBeNull();
        options.AnnotationOption.Name.Should().Be("--annotation");
        options.AnnotationOption.Aliases.Should().Contain("-a");
    }

    [Fact]
    public void PackerOptions_HasConcurrencyOption()
    {
        // Arrange & Act
        var options = new PackerOptions();

        // Assert
        options.ConcurrencyOption.Should().NotBeNull();
        options.ConcurrencyOption.Name.Should().Be("--concurrency");
    }

    [Fact]
    public void PackerOptions_ConcurrencyDefault_IsFive()
    {
        // Arrange
        var options = new PackerOptions();
        var command = new RootCommand();
        options.ApplyTo(command);
        
        // Act
        var parseResult = command.Parse("");
        var defaultValue = parseResult.GetValue(options.ConcurrencyOption);

        // Assert
        defaultValue.Should().Be(3, "default concurrency should be 3");
    }

    [Fact]
    public void TargetOptions_HasTargetOption()
    {
        // Arrange & Act
        var options = new TargetOptions();

        // Assert
        options.TargetOption.Should().NotBeNull();
        options.TargetOption.Name.Should().Be("--target");
        options.TargetOption.Aliases.Should().Contain("-t");
    }

    [Fact]
    public void FormatOptions_HasFormatOption()
    {
        // Arrange & Act
        var options = new FormatOptions();

        // Assert
        options.FormatOption.Should().NotBeNull();
        options.FormatOption.Name.Should().Be("--format");
        options.FormatOption.Aliases.Should().Contain("-f");
    }

    [Fact]
    public void FormatOptions_FormatDefault_IsText()
    {
        // Arrange
        var options = new FormatOptions();
        var command = new RootCommand();
        options.ApplyTo(command);
        
        // Act
        var parseResult = command.Parse("");
        var defaultValue = parseResult.GetValue(options.FormatOption);

        // Assert
        defaultValue.Should().Be("text", "default format should be text");
    }

    [Fact]
    public void FormatOptions_HasPrettyOption()
    {
        // Arrange & Act
        var options = new FormatOptions();

        // Assert
        options.PrettyOption.Should().NotBeNull();
        options.PrettyOption.Name.Should().Be("--pretty");
    }

    [Fact]
    public void PlatformOptions_HasPlatformOption()
    {
        // Arrange & Act
        var options = new PlatformOptions();

        // Assert
        options.PlatformOption.Should().NotBeNull();
        options.PlatformOption.Name.Should().Be("--platform");
    }

    [Fact]
    public void CommonOptions_HasVerboseOption()
    {
        // Arrange & Act
        var options = new CommonOptions();

        // Assert
        options.VerboseOption.Should().NotBeNull();
        options.VerboseOption.Name.Should().Be("--verbose");
        options.VerboseOption.Aliases.Should().Contain("-v");
    }

    [Fact]
    public void CommonOptions_HasDebugOption()
    {
        // Arrange & Act
        var options = new CommonOptions();

        // Assert
        options.DebugOption.Should().NotBeNull();
        options.DebugOption.Name.Should().Be("--debug");
        options.DebugOption.Aliases.Should().Contain("-d");
    }
}

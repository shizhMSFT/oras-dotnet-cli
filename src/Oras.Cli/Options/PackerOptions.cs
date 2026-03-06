using System.CommandLine;

namespace Oras.Options;

/// <summary>
/// Options for packing operations (push, attach).
/// </summary>
public class PackerOptions
{
    public Option<string?> ArtifactTypeOption { get; }
    public Option<string[]?> AnnotationOption { get; }
    public Option<string?> ConfigOption { get; }
    public Option<string[]?> AnnotationFileOption { get; }
    public Option<int> ConcurrencyOption { get; }

    public PackerOptions()
    {
        ArtifactTypeOption = new Option<string?>("--artifact-type")
        {
            Description = "Artifact type for the manifest"
        };

        AnnotationOption = new Option<string[]?>("--annotation", "-a")
        {
            Description = "Manifest annotations (key=value)",
            AllowMultipleArgumentsPerToken = true
        };

        ConfigOption = new Option<string?>("--config")
        {
            Description = "Path to custom config file"
        };

        AnnotationFileOption = new Option<string[]?>("--annotation-file")
        {
            Description = "Path to annotation files",
            AllowMultipleArgumentsPerToken = true
        };

        ConcurrencyOption = new Option<int>("--concurrency")
        {
            Description = "Maximum number of concurrent operations",
            DefaultValueFactory = _ => 3
        };
    }

    public void ApplyTo(Command command)
    {
        command.Add(ArtifactTypeOption);
        command.Add(AnnotationOption);
        command.Add(ConfigOption);
        command.Add(AnnotationFileOption);
        command.Add(ConcurrencyOption);
    }
}

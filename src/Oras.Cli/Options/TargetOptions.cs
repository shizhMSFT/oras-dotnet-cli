using System.CommandLine;

namespace Oras.Options;

/// <summary>
/// Options for specifying target reference (registry/repository:tag).
/// </summary>
public class TargetOptions
{
    public Option<string?> TargetOption { get; }

    public TargetOptions()
    {
        TargetOption = new Option<string?>("--target", "-t")
        {
            Description = "Target reference (e.g., registry/repository:tag)"
        };
    }

    public void ApplyTo(Command command)
    {
        command.Add(TargetOption);
    }
}

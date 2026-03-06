using System.CommandLine;

namespace Oras.Options;

/// <summary>
/// Options for platform selection (os/arch/variant).
/// </summary>
internal class PlatformOptions
{
    public Option<string?> PlatformOption { get; }

    public PlatformOptions()
    {
        PlatformOption = new Option<string?>("--platform")
        {
            Description = "Target platform (os/arch or os/arch/variant)"
        };
    }

    public void ApplyTo(Command command)
    {
        command.Add(PlatformOption);
    }
}

using System.CommandLine;

namespace Oras.Options;

/// <summary>
/// Options for interacting with remote registries (authentication and transport).
/// </summary>
public class RemoteOptions
{
    public Option<bool> PlainHttpOption { get; }
    public Option<bool> InsecureOption { get; }
    public Option<string?> UsernameOption { get; }
    public Option<string?> PasswordOption { get; }
    public Option<bool> PasswordStdinOption { get; }
    public Option<string?> ResolveOption { get; }
    public Option<string?> CaCertOption { get; }
    public Option<string?> HeaderOption { get; }

    public RemoteOptions()
    {
        PlainHttpOption = new Option<bool>("--plain-http")
        {
            Description = "Use plain HTTP for registry communication"
        };

        InsecureOption = new Option<bool>("--insecure")
        {
            Description = "Allow insecure connections to the registry without SSL check"
        };

        UsernameOption = new Option<string?>("--username", "-u")
        {
            Description = "Registry username"
        };

        PasswordOption = new Option<string?>("--password", "-p")
        {
            Description = "Registry password or identity token"
        };

        PasswordStdinOption = new Option<bool>("--password-stdin")
        {
            Description = "Read password or identity token from stdin"
        };

        ResolveOption = new Option<string?>("--resolve")
        {
            Description = "Custom registry resolution (host:port:address)"
        };

        CaCertOption = new Option<string?>("--ca-cert")
        {
            Description = "Path to custom CA certificate file"
        };

        HeaderOption = new Option<string?>("--header", "-H")
        {
            Description = "Add custom headers to requests"
        };
    }

    public void ApplyTo(Command command)
    {
        command.Add(PlainHttpOption);
        command.Add(InsecureOption);
        command.Add(UsernameOption);
        command.Add(PasswordOption);
        command.Add(PasswordStdinOption);
        command.Add(ResolveOption);
        command.Add(CaCertOption);
        command.Add(HeaderOption);
    }
}

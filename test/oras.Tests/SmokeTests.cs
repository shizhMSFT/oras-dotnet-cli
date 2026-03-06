using Xunit;

namespace Oras.Tests;

public class SmokeTests
{
    [Fact]
    public async Task RootCommandShouldExecuteWithoutErrors()
    {
        var exitCode = await Program.Main(["--help"]);

        Assert.Equal(0, exitCode);
    }
}

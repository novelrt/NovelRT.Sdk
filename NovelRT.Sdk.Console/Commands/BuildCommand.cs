using Serilog;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace NovelRT.Sdk.Console;

public class BuildCommand : ICommandHandler
{
    static BuildCommand()
    {
        Command = new Command("build", "Builds a NovelRT project.")
        {
            Handler = new BuildCommand()
        };
    }

    public static Command Command { get; }

    public Task<int> InvokeAsync(InvocationContext context)
    {
        throw new NotImplementedException();
    }
}

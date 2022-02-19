using System.CommandLine;
using System.CommandLine.Invocation;

namespace NovelRT.Sdk.Console;

public class PublishCommand : ICommandHandler
{
    static PublishCommand()
    {
        Command = new Command("publish", "creates and deploys a release build to a specified location.")
        {
            Handler = new PublishCommand()
        };
        
        Command.AddArgument(OutputDirectory);
    }

    public static Argument<string> OutputDirectory { get; } = new Argument<string>("output directory",
        "The directory to store the release build contents in.")
    {
        Arity = ArgumentArity.ExactlyOne
    };
    
    public static Command Command { get; }
    
    public Task<int> InvokeAsync(InvocationContext context)
    {
        var path = context.ParseResult.GetValueForArgument(OutputDirectory);
        
        System.Console.WriteLine($"Publishing to {path}");

        return Task.FromResult(0);
    }
}
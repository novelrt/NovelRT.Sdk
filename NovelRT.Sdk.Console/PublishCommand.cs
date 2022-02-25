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
        Command.AddOption(ProjectDirectory);
    }

    public static Argument<string> OutputDirectory { get; } = new("output directory",
        "The directory to store the release build contents in.")
    {
        Arity = ArgumentArity.ExactlyOne
    };

    public static Option<string> ProjectDirectory { get; } = new(new[] { "-p", "--project" },
        Directory.GetCurrentDirectory,
        "The target project directory to publish from. Assumes current working directory if one is not specified.");

    public static Command Command { get; }
    
    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var path = context.ParseResult.GetValueForArgument(OutputDirectory);
        var projectDir = context.ParseResult.GetValueForOption(ProjectDirectory);
        System.Console.WriteLine($"Publishing to {path}");

        try
        {
            await Publisher.PublishAsync(projectDir!, path);
        }
        catch (IOException e)
        {
            await System.Console.Error.WriteLineAsync("Error: The target directory is not empty. Aborting.");
            return 1;
        }
        
        return 0;
    }
}
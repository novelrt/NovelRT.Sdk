using Serilog;
using Serilog.Events;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace NovelRT.Sdk.Console;

public class PublishCommand : ICommandHandler
{
    private static bool _verbose;
    static PublishCommand()
    {
        Command = new Command("publish", "creates and deploys a release build to a specified location.")
        {
            Handler = new PublishCommand()
        };
        
        Command.AddArgument(OutputDirectory);
        Command.AddOption(ProjectDirectory);
        _verbose = false;
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
        _verbose = context.ParseResult.GetValueForOption(GlobalOptions.VerboseMode);
        if (_verbose)
        {
            Globals.Verbosity.MinimumLevel = LogEventLevel.Debug;
        }

        var path = context.ParseResult.GetValueForArgument(OutputDirectory);
        var projectDir = context.ParseResult.GetValueForOption(ProjectDirectory);
        System.Console.WriteLine($"Publishing to {path}");

        try
        {
            await Publisher.PublishAsync(projectDir!, path, Log.Logger);
        }
        catch (IOException e)
        {
            await System.Console.Error.WriteLineAsync("Error: The target directory is not empty. Aborting.");
            if (Globals.Verbosity.MinimumLevel == LogEventLevel.Verbose)
                Log.Logger.Verbose($"{e.Message}\n{e.StackTrace}");
            return 1;
        }
        
        return 0;
    }
}
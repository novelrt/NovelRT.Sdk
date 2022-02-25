using System.CommandLine;
using System.CommandLine.Invocation;

namespace NovelRT.Sdk.Console;

public class NewCommand : ICommandHandler
{
    static NewCommand()
    {
        Command = new Command("new", "Generates a new NovelRT project.")
        {
            Handler = new NewCommand()
        };

        Command.AddOption(OutputDirectory);
        Command.AddOption(NovelRTVersion);
    }

    public static Command Command { get; }

    public static Option<string> OutputDirectory { get; } = new(new[] { "-o", "--output" },
        Directory.GetCurrentDirectory,
        "The directory in which to generate the project files. Assumes current working directory if not provided.")
    {
        Arity = ArgumentArity.ExactlyOne
    };

    public static Option<Version> NovelRTVersion { get; } =
        new(new[] { "-v", "--version" },
            () => new Version(0, 0, 1), "The NovelRT Engine version to use for this project. Assumes latest if not provided.")
        {
            Arity = ArgumentArity.ExactlyOne
        };

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var outputDirectory = context.ParseResult.GetValueForOption(OutputDirectory);
        var novelrtVersion = context.ParseResult.GetValueForOption(NovelRTVersion);
        System.Console.WriteLine($"Generating in {outputDirectory} with NovelRT version {novelrtVersion!.ToString(3)}");
        try
        {
            await ProjectGenerator.GenerateAsync(outputDirectory!, novelrtVersion!);
        }
        catch (IOException e)
        {
            await System.Console.Error.WriteLineAsync("Error: Project files already exist in this directory! Aborting!");
            return 1;
        }
        return 0;
    }
}
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
    }

    public static Command Command { get; }

    public static Option<string> OutputDirectory { get; } = new(new[] { "-o", "--output" },
        Directory.GetCurrentDirectory,
        "The directory in which to generate the project files. Assumes current working directory if not provided.")
    {
        Arity = ArgumentArity.ExactlyOne
    };

    public Task<int> InvokeAsync(InvocationContext context)
    {
        var result = context.ParseResult.GetValueForOption(OutputDirectory);
        System.Console.WriteLine($"Generating in {result}");
        
        return Task.FromResult(0);
    }
}
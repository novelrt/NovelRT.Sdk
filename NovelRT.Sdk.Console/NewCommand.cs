using Serilog;
using Serilog.Events;
using System.CommandLine;
using System.CommandLine.Invocation;


namespace NovelRT.Sdk.Console;

public class NewCommand : ICommandHandler
{
    private static bool _verbose;

    static NewCommand()
    {
        Command = new Command("new", "Generates a new NovelRT project.")
        {
            Handler = new NewCommand()
        };

        Command.AddOption(OutputDirectory);
        Command.AddOption(NovelRTVersion);
        Command.AddOption(LaunchCMakeConfigure);
        _verbose = false;
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
            () => new Version(0, 0, 1),
            "The NovelRT Engine version to use for this project. Assumes latest if not provided.")
        {
            Arity = ArgumentArity.ExactlyOne
        };

    public static Option<bool> LaunchCMakeConfigure { get; } = new(new[] { "-c", "--configure" }, () => false,
        "configures cmake post-generation in a build folder.");

    public static Option<string> DebugNovelRTVersion { get; } = new(new[] { "-dv", "--debug-version" },
        "Debug versions packaged as custom conan packages. DO NOT USE THIS IF YOU DO NOT KNOW WHAT YOU ARE DOING.");

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        _verbose = context.ParseResult.GetValueForOption(GlobalOptions.VerboseMode);
        if (_verbose)
        {
            Globals.Verbosity.MinimumLevel = LogEventLevel.Debug;
        }

        var outputDirectory = context.ParseResult.GetValueForOption(OutputDirectory);
        var novelrtVersion = context.ParseResult.GetValueForOption(NovelRTVersion);
        var debugNovelrtVersion = context.ParseResult.GetValueForOption(DebugNovelRTVersion);
        var shouldConfigure = context.ParseResult.GetValueForOption(LaunchCMakeConfigure);

        //if (debugNovelrtVersion != null)
        //{
        //    Log.Logger.Warning(
        //        "WARNING: YOU SHOULD NOT BE USING THIS UNLESS YOU KNOW EXACTLY WHAT YOU ARE DOING!");

        //    Log.Logger.Warning(
        //        $"Generating in {outputDirectory} with NovelRT version {debugNovelrtVersion}");
        //    try
        //    {
        //        await ProjectGenerator.GenerateDebugAsync(outputDirectory!, debugNovelrtVersion);
        //    }
        //    catch (IOException e)
        //    {
        //        Log.Logger.Error("Error: Project files already exist in this directory. Aborting.");
        //        if (Globals.Verbosity.MinimumLevel == LogEventLevel.Verbose)
        //            Log.Logger.Verbose($"{e.Message}\n{e.StackTrace}");
        //        return 1;
        //    }
        //}
        //else
        //{
            await EngineSelector.SelectEngineVersion();
            //await EngineSelector.SelectEngineVersion(65263678);
        //    Log.Logger.Information(
        //        $"Generating in {outputDirectory.TrimStart()} with NovelRT version {novelrtVersion!.ToString(3)}");
        //    try
        //    {
        //        await ProjectGenerator.GenerateAsync(outputDirectory!, novelrtVersion!);
        //    }
        //    catch (IOException e)
        //    {
        //        Log.Error("Error: Project files already exist in this directory. Aborting.");
        //        if (_verbose)
        //            Log.Debug($"{e.Message}\n{e.StackTrace}");
        //        return 1;
        //    }
        //}

        //if (shouldConfigure)
        //{
        //    System.Console.WriteLine("Configuring CMake");

        //    await ProjectGenerator.ConfigureAsync(outputDirectory!, Path.Combine(outputDirectory!, "build"),
        //        BuildType.Debug, _verbose);
        //    await ProjectGenerator.ConfigureAsync(outputDirectory!, Path.Combine(outputDirectory!, "build"),
        //        BuildType.Release, _verbose);
        //}

        return 0;
    }
}
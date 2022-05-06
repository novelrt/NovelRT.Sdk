﻿using NovelRT.Sdk.Console.Util;
using NovelRT.Sdk.Project;
using Serilog;
using Serilog.Events;
using System.CommandLine;
using System.CommandLine.Invocation;


namespace NovelRT.Sdk.Console;

public class NewCommand : ICommandHandler
{
    private static bool _verbose;
    private static string _engineLocation = "";
    private static bool _fromSourceBuild = false;

    static NewCommand()
    {
        Command = new Command("new", "Generates a new NovelRT project.")
        {
            Handler = new NewCommand()
        };

        Command.AddOption(EngineLocation);
        Command.AddOption(OutputDirectory);
        //Command.AddOption(NovelRTVersion);
        Command.AddOption(ConfigureProject);
        Command.AddOption(BuildProject);
        _verbose = false;
    }

    public static Command Command { get; }

    public static Option<string> EngineLocation { get; } = new(new[] { "-el", "--engine-location" }, () => "",
        "Path of NovelRT engine/interop to build from source. ONLY USE IF BUILDING NOVELRT!")
    {
        Arity = ArgumentArity.ExactlyOne
    };

    public static Option<string> OutputDirectory { get; } = new(new[] { "-o", "--output" },
        Directory.GetCurrentDirectory,
        "The directory in which to generate the project files. Assumes current working directory if not provided.")
    {
        Arity = ArgumentArity.ExactlyOne
    };

    //public static Option<Version> NovelRTVersion { get; } =
    //    new(new[] { "-v", "--version" },
    //        () => new Version(0, 0, 1),
    //        "The NovelRT Engine version to use for this project. Assumes latest if not provided.")
    //    {
    //        Arity = ArgumentArity.ExactlyOne
    //    };

    public static Option<bool> ConfigureProject { get; } = new(new[] { "-c", "--configure" }, () => false,
        "configures CMake post-generation in a build folder.");

    public static Option<bool> BuildProject { get; } = new(new[] { "-b", "--build" }, () => false,
        "builds project via CMake after configuration.");

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        _verbose = context.ParseResult.GetValueForOption(GlobalOptions.VerboseMode);
        if (_verbose)
        {
            Globals.Verbosity.MinimumLevel = LogEventLevel.Debug;
        }

        string? engineLocation = context.ParseResult.GetValueForOption(EngineLocation);
        string? outputDirectory = context.ParseResult.GetValueForOption(OutputDirectory);
        string novelrtVersion = "";
        bool? shouldConfigure = context.ParseResult.GetValueForOption(ConfigureProject);
        bool? shouldBuild = context.ParseResult.GetValueForOption(BuildProject);
        //var novelrtVersion = context.ParseResult.GetValueForOption(NovelRTVersion);

        //Get definites
        bool willConfigure = (shouldConfigure != null) ? (bool)shouldConfigure : false;
        bool willBuild = (shouldBuild != null) ? (bool)shouldBuild : false;

        if (!string.IsNullOrEmpty(engineLocation))
        {
            Log.Logger.Warning("Warning - Source build of NovelRT was selected.");
            Log.Logger.Warning("Please note that any modifications made to NovelRT may not be supported by the NovelRT team.");
            Log.Logger.Information("Confirming engine location...");

            try
            {
                if (File.Exists(Path.GetFullPath(Path.Combine(engineLocation, "CMakeLists.txt"))))
                {
                    try
                    {
                        var files = Directory.GetFiles(Path.GetFullPath(Path.Combine(engineLocation, "include/NovelRT")), "*.h", SearchOption.AllDirectories);

                        if (files.Length > 0)
                        {
                            //Assume that the location is valid.
                            _engineLocation = engineLocation;
                            _fromSourceBuild = true;
                        }
                        else
                        {
                            throw new InvalidOperationException($"No header files were found at {Path.GetFullPath(Path.Combine(engineLocation, "include/NovelRT"))}");
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Logger.Error("Could not find NovelRT headers in the specified location!");
                        Log.Logger.Error($"{e.Message}");
                        Log.Logger.Debug($"{e.StackTrace}");
                    }
                }
                else
                {
                    throw new InvalidOperationException($"NovelRT CMakeLists file not found at {Path.GetFullPath(Path.Combine(engineLocation, "CMakeLists.txt"))}");
                }
            }
            catch (Exception e)
            {
                Log.Logger.Error("Could not find NovelRT CMakeLists in the specified location!");
                Log.Logger.Error($"{e.Message}");
                Log.Logger.Debug($"{e.StackTrace}");
            }

            Log.Logger.Debug("Confirmed engine location.");

            try
            {
                Log.Logger.Information("Checking for required applications to build NovelRT...");
                
                //Check for CMake and Conan
                await ProgramLocators.FindCMake();
                await ProgramLocators.FindConan();


            }
            catch (Exception e)
            {
                Log.Logger.Error("Something went wrong while trying to check requirements for engine builds!");
                Log.Logger.Error($"{e.Message}");
                Log.Logger.Debug($"{e.StackTrace}");
            }
        }
        else
        {
            try
            {
                _fromSourceBuild = false;
                engineLocation = await EngineSelector.SelectEngineVersion();
            }
            catch (Exception e)
            {
                Log.Logger.Error("Something went wrong while selecting a NovelRT version!");
                Log.Logger.Error($"{e.Message}");
                Log.Logger.Debug($"{e.StackTrace}");
            }
        }

        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Log.Logger.Debug("Setting output directory...");
            outputDirectory = Path.GetFullPath(outputDirectory.TrimStart());
        }
        else
        {
            Log.Logger.Debug("Setting default output directory...");
            outputDirectory = Path.GetFullPath(Environment.CurrentDirectory);
        }

        if (_fromSourceBuild)
        {
            Log.Logger.Information($"Generating project in {outputDirectory} using source build of NovelRT...");

            //Generate project with overrides for from-source builds.
            var project = await ProjectGenerator.GenerateFromSourceAsync(outputDirectory, _engineLocation);
            Log.Logger.Information("Successfully generated new NovelRT project!");

            var buildPath = Path.GetFullPath(Path.Combine(outputDirectory, "build"));

            //Run conan commands for engine
            await ConanHandler.ConfigInstallAsync();
            await ConanHandler.InstallAsync(_engineLocation, buildPath);

            if (willConfigure)
            {
                //Configure project + NovelRT
                await ProjectSourceBuilder.ConfigureAsync(outputDirectory, buildPath, BuildType.Debug, _fromSourceBuild);
            }

            if (willBuild)
            {
                if (!willConfigure)
                {
                    Log.Logger.Warning("Warning - building without specifying configuration flag may cause issues during CMake configuration/building!");
                }

                await ProjectSourceBuilder.BuildAsync(buildPath, _verbose);

                if (!await ProjectSourceBuilder.ConfirmEngineBuildSuccessful(buildPath, project))
                {
                    Log.Logger.Error($"Sommething went wrong while trynig to build NovelRT and the new project.");
                    return -1;
                }
                else
                {
                    Log.Logger.Information("Successfully generated and built project!");
                }
            }

        }
        else
        {
            //Parse NovelRT version from folder path.
            novelrtVersion = Path.GetDirectoryName(engineLocation);

            Log.Logger.Information($"Generating project in {outputDirectory} with NovelRT {novelrtVersion}");

            //Generate project

            if (willConfigure)
            { 
            }

            if (willBuild)
            { 
            }
        }

        

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
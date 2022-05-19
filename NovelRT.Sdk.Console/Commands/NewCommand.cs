using NovelRT.Sdk.Console.Util;
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
        Command.AddOption(NovelRTVersion);
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

    public static Option<String> NovelRTVersion { get; } =
        new(new[] { "--version" },
            () => null,
            "The NovelRT Engine version to use for this project in format 'vx.x.x'. Will prompt if not provided. Ignored if EngineLocation flag is specified.")
        {
            Arity = ArgumentArity.ExactlyOne
        };

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
        string? version = context.ParseResult.GetValueForOption(NovelRTVersion);
        bool? shouldConfigure = context.ParseResult.GetValueForOption(ConfigureProject);
        bool? shouldBuild = context.ParseResult.GetValueForOption(BuildProject);
        
        Version noVersion = new Version(0, 0, 0);
        Version novelrtVersion = noVersion;
        if (!string.IsNullOrEmpty(version))
        { 
            Version.TryParse(version.Substring(1), out novelrtVersion);
            if (novelrtVersion == noVersion)
            {
                Log.Logger.Warning($"Warning - could not process version provided: {version}");
                Log.Logger.Warning($"SDK will now prompt for a proper NovelRT version.");
            }
        }

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
        }
        else
        {
            try
            {
                _fromSourceBuild = false;
                
                if (novelrtVersion == null || novelrtVersion == noVersion)
                {
                    _engineLocation = await EngineSelector.SelectEngineVersion();
                }
                else
                {
                    _engineLocation = await EngineSelector.ChooseSpecificEngineVersion(novelrtVersion);
                }
            }
            catch (Exception e)
            {
                Log.Logger.Error("Something went wrong while selecting a NovelRT version!");
                Log.Logger.Error($"{e.Message}");
                Log.Logger.Debug($"{e.StackTrace}");
                return -1;
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

        string project = "";

        if (_fromSourceBuild)
        {
            Log.Logger.Information($"Generating project in {outputDirectory} using source build of NovelRT...");
            
            //Generate project with overrides for from-source builds.
            project = await ProjectGenerator.GenerateFromSourceAsync(outputDirectory, _engineLocation);
            Log.Logger.Information("Successfully generated new NovelRT project!");
        }
        else
        {
            if (Globals.MinimumSupportedVersion > novelrtVersion)
            {
                Log.Logger.Warning($"Warning - NovelRT {novelrtVersion} is NOT supported. Configuration/building is disabled at this time.");
                willConfigure = false;
                willBuild = false;
            }

            Log.Logger.Information($"Generating project in {outputDirectory} with NovelRT {novelrtVersion}");

            //Generate project
            project = await ProjectGenerator.GenerateAsync(outputDirectory, _engineLocation);
            Log.Logger.Information("Successfully generated new NovelRT project!");
        }

        var buildPath = Path.GetFullPath(Path.Combine(outputDirectory, "build"));

        if (willConfigure || willBuild)
        {
            try
            {
                Log.Logger.Information("Checking for required applications to build your project...");

                //Check for CMake (and Conan if source build)
                await ProgramLocators.FindCMake();
                await ProgramLocators.FindConan();

                //Run conan commands for engine
                await ConanHandler.ConfigInstallAsync();
                await ConanHandler.InstallAsync(outputDirectory, buildPath);
            }
            catch (Exception e)
            {
                Log.Logger.Error("Something went wrong while trying to find required applications!");
                Log.Logger.Error($"{e.Message}");
                Log.Logger.Debug($"{e.StackTrace}");
                Environment.Exit(-1);
            }
        }

        //Configure Engine and Project
        if (willConfigure)
        {
            //Configure project (+ NovelRT)
            await ConanHandler.ConfigureAsync(outputDirectory, buildPath);
        }

        //Build Engine and Project
        if (willBuild)
        {
            await ConanHandler.BuildAsync(outputDirectory, buildPath, _verbose);

            if (!await ProjectSourceBuilder.ConfirmBuildSuccessful(buildPath, project))
            {
                Log.Logger.Error($"Sommething went wrong while trying to build your project.");
                return -1;
            }
            
        }

        Log.Logger.Information("Successfully generated project!");

        return 0;
    }
}
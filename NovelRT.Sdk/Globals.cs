using Serilog;
using Serilog.Core;
using Serilog.Sinks.SystemConsole.Themes;

namespace NovelRT.Sdk
{
    public static class Globals
    {
        public static LoggingLevelSwitch Verbosity = new LoggingLevelSwitch { MinimumLevel = Serilog.Events.LogEventLevel.Information };
        public static string VerboseMessageTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Message:lj}{NewLine}{Exception}";
        public static string InformationalMessageTemplate = "{Message:lj}{NewLine}{Exception}";
        public static Version MinimumSupportedVersion = new Version(0, 1, 0);

        public static ILogger SdkLog = new LoggerConfiguration().WriteTo
            .Console(Serilog.Events.LogEventLevel.Debug, outputTemplate: InformationalMessageTemplate, 
            applyThemeToRedirectedOutput: true)
            .MinimumLevel.ControlledBy(Verbosity).CreateLogger();

        public static Enums.Platform DetermineCurrentPlatform() 
        {
            if (OperatingSystem.IsWindows())
            {
                return Enums.Platform.Win32;
            }
            else if (OperatingSystem.IsMacOS())
            {
                return Enums.Platform.macOS;
            }
            else if (OperatingSystem.IsLinux())
            {
                return Enums.Platform.Linux;
            }
            else
                return Enums.Platform.Unknown;
        }
        
    }
}

using System.CommandLine;
using System.Diagnostics;
using NovelRT.Sdk;
using NovelRT.Sdk.Console;
using Serilog;

Log.Logger = Globals.SdkLog;

var rootCommand = new RootCommand
{
    Description = "NovelRT SDK CLI"
};
rootCommand.AddGlobalOption(GlobalOptions.VerboseMode);
rootCommand.AddCommand(NewCommand.Command);
rootCommand.AddCommand(BuildCommand.Command);

if (Debugger.IsAttached)
{
    var arg = new string[] { "new", "-o", @"D:\test\appNo", "-c", "-b" };//, "--verbose" };
    var rc = rootCommand.Invoke(arg);
    Log.CloseAndFlush();
    return rc;
}
else
{
    var rc = rootCommand.Invoke(args);
    Log.CloseAndFlush();
    return rc;
}

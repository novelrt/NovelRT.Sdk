// See https://aka.ms/new-console-template for more information

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
rootCommand.AddCommand(PublishCommand.Command);

if (Debugger.IsAttached)
{
    var arg = new string[] { "new", "-o", @"D:\test\appNo", "--version", "v0.1.0", "-c", "-b" };//, "--verbose" };
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

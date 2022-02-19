// See https://aka.ms/new-console-template for more information

using System.CommandLine;
using NovelRT.Sdk.Console;

var rootCommand = new RootCommand
{
    Description = "NovelRT SDK CLI"
};

rootCommand.AddCommand(NewCommand.Command);
rootCommand.AddCommand(PublishCommand.Command);

return rootCommand.Invoke(args);
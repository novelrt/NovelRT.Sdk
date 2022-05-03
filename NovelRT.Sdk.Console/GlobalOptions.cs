
using System.CommandLine;

namespace NovelRT.Sdk.Console
{
    public class GlobalOptions
    {
        public static Option<bool> VerboseMode { get; } = new(new[] { "--verbose", "-v" }, () => false,
        "Enables verbose output")
        {
            Arity = ArgumentArity.Zero
        };
    }
}

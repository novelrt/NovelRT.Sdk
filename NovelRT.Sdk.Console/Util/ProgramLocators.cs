using Serilog;
using System.Diagnostics;

namespace NovelRT.Sdk.Console.Util
{
    public static class ProgramLocators
    {
        private static bool _cmakeFound = false;
        private static bool _conanFound = false;

        public static async Task<bool> FindCMake()
        {
            var options = new ProcessStartInfo();
            options.RedirectStandardOutput = true;
            options.UseShellExecute = false;
            options.FileName = "cmake";
            options.Arguments = "--version";
            var proc = new Process();
            proc.StartInfo = options;
            proc.OutputDataReceived += new DataReceivedEventHandler(async (o, e) => await ParseCMakeOutput(e.Data));
            proc.Start();
            proc.BeginOutputReadLine();
            await proc.WaitForExitAsync();

            return _cmakeFound;
        }

        public static async Task<bool> FindConan()
        {
            var options = new ProcessStartInfo();
            options.RedirectStandardOutput = true;
            options.UseShellExecute = false;
            options.FileName = "conan";
            options.Arguments = "--version";
            var proc = new Process();
            proc.StartInfo = options;
            proc.OutputDataReceived += new DataReceivedEventHandler(async (o, e) => await ParseConanOutput(e.Data));
            proc.Start();
            proc.BeginOutputReadLine();
            await proc.WaitForExitAsync();

            return _conanFound;
        }

        private static async Task ParseCMakeOutput(string? input)
        {
            if (!_cmakeFound && !string.IsNullOrEmpty(input))
            {
                var version = input.Substring(14, input.Length - 14);
                Version ver = new Version(version);
                if (ver < new Version(3, 19, 8))
                {
                    throw new NotSupportedException($"CMake {version} is not compatible with NovelRT at this time. Please use version 3.19.8 or above.");
                }
                else
                {
                    Log.Logger.Information($"Found CMake version {version}!");
                    _cmakeFound = true;
                }
            }
        }

        private static async Task ParseConanOutput(string? input)
        {
            if (!_conanFound && !string.IsNullOrEmpty(input))
            {
                var version = input.Substring(14, input.Length - 14);
                Version ver = new Version(version);
                if (ver < new Version(1, 43, 0))
                {
                    throw new NotSupportedException($"Conan {version} is not compatible with NovelRT at this time. Please use version 1.43.0 or above.");
                }
                else
                {
                    Log.Logger.Information($"Found Conan version {version}!");
                    _conanFound = true;
                }
            }
        }

    }
}

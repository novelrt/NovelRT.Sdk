using System.Diagnostics;
using static NovelRT.Sdk.Globals;

namespace NovelRT.Sdk.Project
{
    public class ProjectSourceBuilder
    {
        private static bool _appBuiltProperly = false;
        private static OperatingSystem _operatingSystem;
        private static bool _verbose = false;

        public static async Task BuildAsync(string projectBuildLocation, bool verboseBuild)
        {
            _operatingSystem = Environment.OSVersion;
            _verbose = verboseBuild;

            var start = new ProcessStartInfo
            {
                FileName = "cmake",
                Arguments = $"--build {projectBuildLocation}",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };

            var proc = new Process();
            proc.StartInfo = start;
            proc.OutputDataReceived += new DataReceivedEventHandler(async (o, e) => await ParseBuildProgressAsync(e.Data));
            proc.ErrorDataReceived += new DataReceivedEventHandler(async (o, e) => await ParseBuildErrorAsync(e.Data));

            SdkLog.Information("Building project...");

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            await proc.WaitForExitAsync();
        }

        public static async Task<bool> ConfirmEngineBuildSuccessful(string projectBuildLocation, string projectName)
        {
            var appName = projectName;
            if (OperatingSystem.IsOSPlatform("windows"))
            {
                appName += ".exe";
            }

            var path = $"{projectBuildLocation}/src/{projectName}/Debug";
            path = path.Replace("\\", "/");
            var start = new ProcessStartInfo
            {
                FileName = $"{path}/{appName}",
                WorkingDirectory = path,
                RedirectStandardOutput = true,
            };

            var proc = new Process();
            proc.StartInfo = start;
            proc.OutputDataReceived += new DataReceivedEventHandler(async (o, e) => await ParseHelloWorldAsync(e.Data));
            SdkLog.Information("Checking for finished build...");

            proc.Start();
            proc.BeginOutputReadLine();
            await proc.WaitForExitAsync();

            return _appBuiltProperly;
        }

        public static async Task ConfigureAsync(string projectLocation, string projectOutputDir, BuildType buildType, bool fromSource = false)
        {
            var args = $"-S { projectLocation } -B { projectOutputDir }";
            if (fromSource)
            {
                SdkLog.Warning("Warning - automatically disabling documentation and sample generation for Engine builds.");
                args += " -DNOVELRT_BUILD_DOCUMENTATION=OFF -DNOVELRT_BUILD_SAMPLES=OFF";
            }

            var start = new ProcessStartInfo
            {
                FileName = "cmake",
                Arguments = args,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            var proc = new Process();
            proc.StartInfo = start;
            proc.OutputDataReceived += new DataReceivedEventHandler((o, e) => SdkLog.Debug(e.Data));
            proc.ErrorDataReceived += new DataReceivedEventHandler((o, e) => SdkLog.Error(e.Data));

            SdkLog.Information("Configuring project...");

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            await proc.WaitForExitAsync();
        }

        private static async Task ParseHelloWorldAsync(string? data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                SdkLog.Information(data);
                if (data.Contains("Hello"))
                {
                    _appBuiltProperly = true;
                }
            }
        }

        private static async Task ParseBuildProgressAsync(string? data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                if (_verbose)
                {
                    SdkLog.Debug($"{data}");
                }
                else
                {
                    if (_operatingSystem.Platform == PlatformID.Win32NT)
                    {
                        if (data.Contains(".dll") || data.Contains(".exe"))
                        {
                            var indexOne = data.LastIndexOf('\\') + 1;
                            var length = data.Length - indexOne;
                            var builtFile = data.Substring(indexOne + 1, length);
                            SdkLog.Information($"Finished building {builtFile}");
                        }
                    }
                    else
                    {
                        if (data.Contains("%]"))
                        {
                            var indexOne = data.IndexOf(']');
                            var progress = data.Substring(0, indexOne + 1);
                            SdkLog.Information($"Building... {progress}");
                        }
                    }
                }
            }
        }

        private static async Task ParseBuildErrorAsync(string? data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                SdkLog.Error($"{data}");
            }
        }
    }
}

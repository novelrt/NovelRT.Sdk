using static NovelRT.Sdk.Globals;
using System.Diagnostics;

namespace NovelRT.Sdk.Project
{
    public static class ConanHandler
    {
        private static List<string> _availableConfigs = new List<string>();
        private static string _selectedConfig = "";

        public static async Task ConfigInstallAsync(string url = "https://github.com/NovelRT/ConanConfig.git")
        {
            SdkLog.Information($"Downloading available Conan configurations from: {url}...");
            var args = $"config install {url}";

            var start = new ProcessStartInfo
            {
                FileName = "conan",
                Arguments = args,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            var proc = new Process();
            proc.StartInfo = start;
            proc.OutputDataReceived += new DataReceivedEventHandler(async (o, e) => await ParseConfigInstallOutput(e.Data));
            proc.ErrorDataReceived += new DataReceivedEventHandler((o, e) => SdkLog.Error(e.Data));

            proc.Start();
            proc.BeginOutputReadLine();
            //proc.BeginErrorReadLine();
            await proc.WaitForExitAsync();

            if (proc.ExitCode != 0)
            {
                    
            }
        }

        public static async Task InstallAsync(string conanfilePath, string projectOutputDir)
        {
            _selectedConfig = $"{await DeterminePlatformForConfigAsync()}";

            var args = $"install {conanfilePath} -if {projectOutputDir} --build=missing -pr {_selectedConfig}";

            var start = new ProcessStartInfo
            {
                FileName = "conan",
                Arguments = args,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            var proc = new Process();
            proc.StartInfo = start;
            proc.OutputDataReceived += new DataReceivedEventHandler((o, e) => SdkLog.Debug(!string.IsNullOrEmpty(e.Data) ? e.Data : ""));
            proc.ErrorDataReceived += new DataReceivedEventHandler((o, e) => SdkLog.Warning(!string.IsNullOrEmpty(e.Data) ? e.Data : ""));

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            await proc.WaitForExitAsync();
        }

        public static async Task ConfigureAsync(string conanfilePath, string projectOutputDir)
        {
            var args = $"build {conanfilePath} --build-folder {projectOutputDir} --configure";

            var start = new ProcessStartInfo
            {
                FileName = "conan",
                Arguments = args,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            var proc = new Process();
            proc.StartInfo = start;
            proc.OutputDataReceived += new DataReceivedEventHandler((o, e) => SdkLog.Debug(!string.IsNullOrEmpty(e.Data) ? e.Data : ""));
            proc.ErrorDataReceived += new DataReceivedEventHandler((o, e) => SdkLog.Warning(!string.IsNullOrEmpty(e.Data) ? e.Data : ""));

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            await proc.WaitForExitAsync();
        }


        public static async Task BuildAsync(string conanfilePath, string projectOutputDir)
        {
            var args = $"build {conanfilePath} --build-folder {projectOutputDir} --build";

            var start = new ProcessStartInfo
            {
                FileName = "conan",
                Arguments = args,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            var proc = new Process();
            proc.StartInfo = start;
            proc.OutputDataReceived += new DataReceivedEventHandler((o, e) => SdkLog.Debug(!string.IsNullOrEmpty(e.Data) ? e.Data : ""));
            proc.ErrorDataReceived += new DataReceivedEventHandler((o, e) => SdkLog.Warning(!string.IsNullOrEmpty(e.Data) ? e.Data : ""));

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            await proc.WaitForExitAsync();
        }

        private static async Task ParseConfigInstallOutput(string? data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                SdkLog.Debug(data);
                if (data.Contains("profiles"))
                {
                    var strings = data.Split(' ', StringSplitOptions.TrimEntries);
                    var profile = strings[2];
                    _availableConfigs.Add(profile);
                }
            }
        }

        private static async Task<string> DeterminePlatformForConfigAsync()
        {
            SdkLog.Information("Please choose a configuration that is applicable to your build system.");
            SdkLog.Warning("Note: choosing an invalid configuration will cause the build and configure options to fail.\n");

            string id = "";

            if (OperatingSystem.IsOSPlatform("macOS"))
            {
                id = "macOS";
            }
            else if (OperatingSystem.IsOSPlatform("Linux"))
            {
                id = "linux";
            }
            else
            {
                id = "windows";
            }

            Dictionary<int, string> validConfigs = new Dictionary<int, string>();
            int inc = 1;
            foreach (var config in _availableConfigs)
            {
                if (config.Contains(id) && config.Count(c => c == '-') < 3)
                { 
                    validConfigs.Add(inc, config);
                    SdkLog.Information($"{inc}. {config}");
                    inc++;
                }
            }
            int choice = 0;

            while (choice == 0)
            {
                Console.Write("\nPlease select a proper number or Q to quit: ");
                string selection = Console.ReadLine();
                if (!int.TryParse(selection, out choice) || choice > validConfigs.Count)
                {
                    if (selection.Contains('Q') || selection.Contains('q'))
                    {
                        SdkLog.Information("Exiting...");
                        Environment.Exit(0);
                    }
                    SdkLog.Error("Invalid selection - please try again.\n");
                    choice = 0;

                    foreach (var c in validConfigs)
                    {
                        SdkLog.Information($"{c.Key}. {c.Value}");
                    }
                }
            }

            return validConfigs[choice];
        }
    }
}

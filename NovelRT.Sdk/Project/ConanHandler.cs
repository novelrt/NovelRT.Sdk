using static NovelRT.Sdk.Globals;
using System.Diagnostics;
using NovelRT.Sdk.Models;
using System.Text.Json;

namespace NovelRT.Sdk.Project
{
    public static class ConanHandler
    {
        private static List<string> _availableConfigs = new List<string>();
        private static string _selectedConfig = "";
        private static bool _verbose = false;
        private static Enums.Platform _currentPlatform = Enums.Platform.Unknown;

        public static async Task ConfigInstallAsync(string url = "https://github.com/NovelRT/ConanConfig.git")
        {
            SdkLog.Information($"\nDownloading available Conan configurations from: {url}...");
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

            proc.Start();
            proc.BeginOutputReadLine();
            await proc.WaitForExitAsync();
        }

        public static async Task InstallAsync(string conanfilePath, string projectOutputDir)
        {
            _selectedConfig = $"{await DeterminePlatformForConfigAsync()}";
            SdkLog.Information("Updating dependencies...");

            await ModifyProjectMetadata(conanfilePath, outputDirectory: projectOutputDir, profile: _selectedConfig);
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
            SdkLog.Information("Configuring project...");
            await ModifyProjectMetadata(conanfilePath, outputDirectory: projectOutputDir, buildApp: "conan");
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

        public static async Task BuildAsync(string conanfilePath, string projectOutputDir, bool verbose)
        {
            
            SdkLog.Information("Building project...");
            _verbose = verbose;
            _currentPlatform = Globals.DetermineCurrentPlatform();
            var args = $"build {conanfilePath} --build-folder {projectOutputDir} --build";
            await ModifyProjectMetadata(conanfilePath, outputDirectory: projectOutputDir, buildApp: "conan", buildAppArgs: args);
            
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
            proc.OutputDataReceived += new DataReceivedEventHandler(async (o, e) => await ParseBuildOutput(e.Data));
            proc.ErrorDataReceived += new DataReceivedEventHandler((o, e) => SdkLog.Debug(!string.IsNullOrEmpty(e.Data) ? e.Data : ""));

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

        private static async Task ParseBuildOutput(string? data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                if (_verbose)
                {
                    SdkLog.Debug($"{data}");
                }
                else
                {
                    if (_currentPlatform == Enums.Platform.Win32)
                    {
                        if (data.Contains(".dll") || data.Contains(".exe"))
                        {
                            var indexOne = data.LastIndexOf('\\') + 1;
                            var length = data.Length - indexOne;
                            var builtFile = data.Substring(indexOne, length);
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

        private static async Task<ProjectDefinition> ModifyProjectMetadata(string path, string? buildApp = null, string? buildAppArgs = null, string? outputDirectory = null, string? profile = null)
        {
            SdkLog.Debug("Gathering project metadata...");
            bool fileChanged = false;

            var projectFile = Path.Combine(path, "project.json");
            if (!File.Exists(projectFile))
            {
                throw new FileNotFoundException("Could not find project.json in provided directory! Is this a proper NovelRT SDK project?");
            }
            string jsonText = File.ReadAllText(projectFile);
            ProjectDefinition? def = JsonSerializer.Deserialize<ProjectDefinition>(jsonText);
            
            if (def == null)
            {
                throw new InvalidDataException("Could not deserialize project.json!");
            }

            if (!string.IsNullOrEmpty(buildApp) && def.BuildApp != buildApp)
            {
                def.BuildApp = buildApp;
                fileChanged = true;
            }
            if (!string.IsNullOrEmpty(buildAppArgs) && def.BuildAppArgs != buildAppArgs)
            {
                def.BuildAppArgs = buildAppArgs;
                fileChanged = true;
            }
            if (!string.IsNullOrEmpty(outputDirectory) && def.OutputDirectory != outputDirectory)
            {
                def.OutputDirectory = outputDirectory;
                fileChanged = true;
            }
            if (!string.IsNullOrEmpty(profile) && def.DependencyProfile != profile)
            {
                def.DependencyProfile = profile;
                fileChanged = true;
            }

            if (fileChanged)
            { 
                File.WriteAllText(projectFile, JsonSerializer.Serialize(def, new JsonSerializerOptions { WriteIndented = true }));
            }

            return def;
        }
    }
}

using static NovelRT.Sdk.Globals;
using System.Diagnostics;

namespace NovelRT.Sdk;

public class ProjectBuilder
{
    private static bool _appBuiltProperly = false;

    public static async Task BuildAsync(string projectBuildLocation)
    {
        var start = new ProcessStartInfo
        {
            FileName = "cmake",
            Arguments = $"--build {projectBuildLocation}",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        var proc = new Process();
        proc.StartInfo = start;
        proc.OutputDataReceived += new DataReceivedEventHandler((o, e) => SdkLog.Debug("{Data}", e.Data != null ? e.Data : ""));
        proc.ErrorDataReceived += new DataReceivedEventHandler((o, e) => SdkLog.Error("{Data}", e.Data != null ? e.Data : ""));

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
        proc.OutputDataReceived += new DataReceivedEventHandler(async (o, e) => await ParseHelloWorld(e.Data));

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


    private static async Task ParseHelloWorld(string? data)
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
}
using static NovelRT.Sdk.Globals;
using System.Diagnostics;

namespace NovelRT.Sdk;

public class ProjectBuilder
{
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
        proc.OutputDataReceived += new DataReceivedEventHandler((o, e) => SdkLog.Debug(e.Data != null ? e.Data : ""));
        proc.ErrorDataReceived += new DataReceivedEventHandler((o, e) => SdkLog.Error(e.Data != null ? e.Data : ""));

        proc.Start();
        await proc.WaitForExitAsync();
    }

    public static async Task ConfigureAsync(string projectLocation, string projectOutputDir, BuildType buildType)
    {
        var args = $"-S { projectLocation } -B { projectOutputDir }";

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
        proc.OutputDataReceived += new DataReceivedEventHandler((o, e) => SdkLog.Information(e.Data));
        proc.ErrorDataReceived += new DataReceivedEventHandler((o, e) => SdkLog.Error(e.Data));

        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        await proc.WaitForExitAsync();
    }

}
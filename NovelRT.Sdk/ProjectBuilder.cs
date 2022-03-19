using System.Diagnostics;

namespace NovelRT.Sdk;

public class ProjectBuilder
{
    public static async Task BuildAsync(string projectBuildLocation)
    {
        await Process.Start($"cmake --build {projectBuildLocation}").WaitForExitAsync();
    }
}
using static NovelRT.Sdk.Globals;
using System.Diagnostics;

namespace NovelRT.Sdk;

public static class ProjectGenerator
{
    public static async Task ConfigureAsync(string projectLocation, string projectOutputDir, BuildType buildType, bool verboseMode = false)
    {
        var args = $"-S { projectLocation } -B { projectOutputDir }";
        if (verboseMode)
            args += " --verbose";

        var start = new ProcessStartInfo
        {
            FileName = "cmake",
            Arguments = args,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = true
        };

        var proc = new Process();
        proc.StartInfo = start;
        proc.OutputDataReceived += new DataReceivedEventHandler((o, e) => SdkLog.Information(e.Data));
        proc.ErrorDataReceived += new DataReceivedEventHandler((o, e) => SdkLog.Error(e.Data));

        proc.Start();
        await proc.WaitForExitAsync();
    }
    
    public static async Task GenerateDebugAsync(string newProjectPath, string novelrtVersion)
    {
        var templateFilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TemplateFiles");
        var cmakeTemplatePath = Path.Combine(templateFilesPath , "CMakeTemplate");

        SdkLog.Debug($"Attempting to copy template to {newProjectPath}.");
        
        await CopyDirectoryAsync(cmakeTemplatePath, newProjectPath, true);
        var projectName = new DirectoryInfo(newProjectPath).Name;
        var projectDescription = $"{projectName} app";
        var projectVersionString = "0.0.1";
        var novelrtVersionString = novelrtVersion;

        await DeletePlaceholderFilesWithDirectoryLoggingAsync(new DirectoryInfo(newProjectPath), projectName);
        await OverwriteCMakeTemplateVariableDataAsync(new DirectoryInfo(newProjectPath), projectName!, newProjectPath,
            projectDescription, projectVersionString, novelrtVersionString);

        var conanfilePath = newProjectPath + Path.DirectorySeparatorChar + "conanfile.py";
        File.Copy(templateFilesPath + Path.DirectorySeparatorChar + "conanfile.py", conanfilePath);
        
        SdkLog.Information($"Generating {conanfilePath}");
        var conanfileContents = await File.ReadAllTextAsync(conanfilePath);
        conanfileContents = ApplyProjectContextToFile(projectName!, projectDescription, projectVersionString, novelrtVersionString, conanfileContents);
        await File.WriteAllTextAsync(conanfilePath, conanfileContents);
    }
    
    public static async Task GenerateAsync(string newProjectPath, Version novelrtVersion)
    {
        string projectPath;
        if (newProjectPath != null)
            projectPath = newProjectPath.TrimStart();
        else
            projectPath = Directory.GetCurrentDirectory();

        var templateFilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TemplateFiles");
        var cmakeTemplatePath = Path.Combine(templateFilesPath , "CMakeTemplate");
        
        await CopyDirectoryAsync(cmakeTemplatePath, projectPath, true);
        var projectName = new DirectoryInfo(projectPath).Name;
        var projectDescription = $"{projectName} app";
        var projectVersionString = "0.0.1";
        var novelrtVersionString = novelrtVersion.ToString(3);

        await DeletePlaceholderFilesWithDirectoryLoggingAsync(new DirectoryInfo(projectPath), projectName);
        await OverwriteCMakeTemplateVariableDataAsync(new DirectoryInfo(projectPath), projectName!, projectPath,
            projectDescription, projectVersionString, novelrtVersionString);

        var conanfilePath = projectPath + Path.DirectorySeparatorChar + "conanfile.py";
        File.Copy(templateFilesPath + Path.DirectorySeparatorChar + "conanfile.py", conanfilePath);
        
        SdkLog.Information($"Generating {conanfilePath}");
        var conanfileContents = await File.ReadAllTextAsync(conanfilePath);
        conanfileContents = ApplyProjectContextToFile(projectName!, projectDescription, projectVersionString, novelrtVersionString, conanfileContents);
        await File.WriteAllTextAsync(conanfilePath, conanfileContents);
    }

    private static async Task DeletePlaceholderFilesWithDirectoryLoggingAsync(DirectoryInfo projectPath, string projectName)
    {
        foreach (DirectoryInfo subDir in projectPath.GetDirectories())
        {
            await DeletePlaceholderFilesWithDirectoryLoggingAsync(subDir, projectName);
            var placeholderFiles = subDir.GetFiles("DeleteMe.txt");

            foreach (var placeholderFile in placeholderFiles)
            {
                SdkLog.Information($"Generating {placeholderFile.DirectoryName!.Replace("PROJECT_NAME", projectName)}");
                placeholderFile.Delete();
            }
        }
    }

    private static async Task OverwriteCMakeTemplateVariableDataAsync(DirectoryInfo rootDir, string projectName, string projectPath, string projectDescription, string projectVersion, string novelrtVersion)
    {
        DirectoryInfo[] directories = rootDir.GetDirectories();

        foreach (var dir in directories)
        {
            if (dir.Name == "PROJECT_NAME")
            {
                var newPath = dir.FullName.Replace("PROJECT_NAME", projectName);
                dir.MoveTo(newPath);
            }
            
            await OverwriteCMakeTemplateVariableDataAsync(dir, projectName, projectPath, projectDescription, projectVersion, novelrtVersion);
        }

        var cmakeFiles = rootDir.GetFiles("*.txt");
        
        
        foreach (var cmakeFile in cmakeFiles)
        {
            SdkLog.Information($"Generating {cmakeFile}");
            string fileContents = await File.ReadAllTextAsync(cmakeFile.FullName);
            fileContents = ApplyProjectContextToFile(projectName, projectDescription, projectVersion, novelrtVersion, fileContents);
            fileContents = fileContents.Replace($"{projectName}_NOVELRT_VERSION",
                $"{projectName}_NOVELRT_VERSION".ToUpperInvariant());
            await File.WriteAllTextAsync(cmakeFile.FullName, fileContents);
        }
    }

    private static string ApplyProjectContextToFile(string projectName, string projectDescription, string projectVersion,
        string novelrtVersion, string fileContents)
    {
        fileContents = fileContents.Replace("###PROJECT_NAME###", projectName);
        fileContents = fileContents.Replace("###PROJECT_DESCRIPTION###", projectDescription);
        fileContents = fileContents.Replace("###PROJECT_VERSION###", projectVersion);
        fileContents = fileContents.Replace("###NOVELRT_VERSION###", novelrtVersion);
        return fileContents;
    }

    private static async Task CopyDirectoryAsync(string sourceDir, string destinationDir, bool recursive)
    {
        SdkLog.Debug("CopyDirectoryAsync - ProjectGenerator");
        SdkLog.Debug($"Source Directory: {sourceDir}");
        SdkLog.Debug($"Destination Directory: {destinationDir}");
        var dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        DirectoryInfo[] dirs = dir.GetDirectories();
        var path = Path.GetFullPath(destinationDir);
        if (!Directory.Exists(path))
            Directory.CreateDirectory(destinationDir);

        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            var copied = file.CopyTo(targetFilePath);
            if(SdkLog != null)
                SdkLog.Debug($"\tCopied {copied.FullName}");
        }

        if (recursive)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                await CopyDirectoryAsync(subDir.FullName, newDestinationDir, true);
            }
        }
    }
}
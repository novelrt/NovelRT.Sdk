using System.Diagnostics;
using System.Reflection;

namespace NovelRT.Sdk;

public static class ProjectGenerator
{
    public static async Task ConfigureAsync(string projectLocation, string projectOutputDir, BuildType buildType)
    {
        await Process.Start($"cmake -S {projectLocation} -B {projectOutputDir}").WaitForExitAsync();
    }
    
    public static async Task GenerateDebugAsync(string newProjectPath, string novelrtVersion)
    {
        var templateFilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TemplateFiles");
        var cmakeTemplatePath = Path.Combine(templateFilesPath , "CMakeTemplate");
        
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
        
        Console.WriteLine($"Generating {conanfilePath}");
        var conanfileContents = await File.ReadAllTextAsync(conanfilePath);
        conanfileContents = ApplyProjectContextToFile(projectName!, projectDescription, projectVersionString, novelrtVersionString, conanfileContents);
        await File.WriteAllTextAsync(conanfilePath, conanfileContents);
    }
    
    public static async Task GenerateAsync(string newProjectPath, Version novelrtVersion)
    {
        var templateFilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TemplateFiles");
        var cmakeTemplatePath = Path.Combine(templateFilesPath , "CMakeTemplate");
        
        await CopyDirectoryAsync(cmakeTemplatePath, newProjectPath, true);
        var projectName = new DirectoryInfo(newProjectPath).Name;
        var projectDescription = $"{projectName} app";
        var projectVersionString = "0.0.1";
        var novelrtVersionString = novelrtVersion.ToString(3);

        await DeletePlaceholderFilesWithDirectoryLoggingAsync(new DirectoryInfo(newProjectPath), projectName);
        await OverwriteCMakeTemplateVariableDataAsync(new DirectoryInfo(newProjectPath), projectName!, newProjectPath,
            projectDescription, projectVersionString, novelrtVersionString);

        var conanfilePath = newProjectPath + Path.DirectorySeparatorChar + "conanfile.py";
        File.Copy(templateFilesPath + Path.DirectorySeparatorChar + "conanfile.py", conanfilePath);
        
        Console.WriteLine($"Generating {conanfilePath}");
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
                Console.WriteLine($"Generating {placeholderFile.DirectoryName!.Replace("PROJECT_NAME", projectName)}");
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
            Console.WriteLine($"Generating {cmakeFile}");
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
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        DirectoryInfo[] dirs = dir.GetDirectories();
        Directory.CreateDirectory(destinationDir);

        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
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
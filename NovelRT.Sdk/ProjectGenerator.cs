namespace NovelRT.Sdk;

public static class ProjectGenerator
{
    public static async Task GenerateAsync(string newProjectPath)
    {
        var templateFilesPath = "TemplateFiles";
        var cmakeTemplatePath = templateFilesPath + Path.DirectorySeparatorChar + "CMakeTemplate";
        
        await CopyDirectoryAsync(cmakeTemplatePath, newProjectPath, true);
        var projectName = new DirectoryInfo(newProjectPath).Name;
        var projectDescription = $"{projectName} game";
        var projectVersion = "0.0.1";
        var novelrtVersion = "0.0.1";

        await DeletePlaceholderFilesAsync(new DirectoryInfo(newProjectPath));
        await OverwriteCMakeTemplateVariableDataAsync(new DirectoryInfo(newProjectPath), projectName!, newProjectPath,
            projectDescription, projectVersion, novelrtVersion);

        var conanfilePath = newProjectPath + Path.DirectorySeparatorChar + "conanfile.py";
        File.Copy(templateFilesPath + Path.DirectorySeparatorChar + "conanfile.py", conanfilePath);
        
        Console.WriteLine($"Generating {conanfilePath}");
        var conanfileContents = await File.ReadAllTextAsync(conanfilePath);
        conanfileContents = ApplyProjectContextToFile(projectName!, projectDescription, projectVersion, novelrtVersion, conanfileContents);
        await File.WriteAllTextAsync(conanfilePath, conanfileContents);
    }

    private static async Task DeletePlaceholderFilesAsync(DirectoryInfo projectPath)
    {
        foreach (DirectoryInfo subDir in projectPath.GetDirectories())
        {
            await DeletePlaceholderFilesAsync(subDir);
            var placeholderFiles = subDir.GetFiles("DeleteMe.txt");

            foreach (var placeholderFile in placeholderFiles)
            {
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
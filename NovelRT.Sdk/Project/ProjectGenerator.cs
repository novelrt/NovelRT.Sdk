using static NovelRT.Sdk.Globals;
using System.Diagnostics;

namespace NovelRT.Sdk;

public static class ProjectGenerator
{
    private static bool _fromSource = false;

    public static async Task<string> GenerateFromSourceAsync(string newProjectPath, string novelrtPath)
    {
        _fromSource = true;
        var templateFilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TemplateFiles");
        var cmakeTemplatePath = Path.Combine(templateFilesPath , "CMakeTemplate");

        SdkLog.Debug($"Attempting to copy template to {newProjectPath}.");
        
        await CopyDirectoryAsync(cmakeTemplatePath, newProjectPath, true);
        var projectName = new DirectoryInfo(newProjectPath).Name;
        var projectDescription = $"{projectName} app";
        var projectVersionString = "0.0.1";
        //var novelrtVersionString = novelrtVersion;

        await DeletePlaceholderFilesWithDirectoryLoggingAsync(new DirectoryInfo(newProjectPath), projectName);
        await OverwriteCMakeTemplateVariableDataAsync(new DirectoryInfo(newProjectPath), projectName!, newProjectPath,
            projectDescription, projectVersionString, novelrtPath);

        return projectName;
    }
    
    public static async Task<string> GenerateAsync(string newProjectPath, Version novelrtVersion)
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
        return projectName;
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

    private static async Task OverwriteCMakeTemplateVariableDataAsync(DirectoryInfo rootDir, string projectName, string projectPath, string projectDescription, string projectVersion, string novelrtPath)
    {
        DirectoryInfo[] directories = rootDir.GetDirectories();

        foreach (var dir in directories)
        {
            if (dir.Name == "PROJECT_NAME")
            {
                var newPath = dir.FullName.Replace("PROJECT_NAME", projectName);
                dir.MoveTo(newPath);
            }
            
            await OverwriteCMakeTemplateVariableDataAsync(dir, projectName, projectPath, projectDescription, projectVersion, novelrtPath);
        }

        var cmakeFiles = rootDir.GetFiles("*.txt");
        
        
        foreach (var cmakeFile in cmakeFiles)
        {
            SdkLog.Information($"Generating {cmakeFile}");

            string fileContents = await File.ReadAllTextAsync(cmakeFile.FullName);

            fileContents = ApplyProjectContextToFile(projectName, projectDescription, projectVersion, novelrtPath, fileContents);

            var path = novelrtPath.Replace("\\", "/");
            var includePath = Path.Combine(path, "include").Replace("\\", "/");
            string cmakePath = cmakeFile.FullName.Replace(cmakeFile.Name, "build/engine").Replace("\\", "/");
            fileContents = fileContents.Replace($"###NOVELRT_ENGINE_SUBDIR###",
                $"include_directories(\"{includePath}\")" +
                $"\nadd_subdirectory(\"{path}\" \"{cmakePath}\")" +
                $"\nset_target_properties(Engine PROPERTIES" +
                $"\n\tMAP_IMPORTED_CONFIG_DEBUG RelWithDebInfo)");

            await File.WriteAllTextAsync(cmakeFile.FullName, fileContents);
        }
    }

    private static string ApplyProjectContextToFile(string projectName, string projectDescription, string projectVersion,
        string novelrtVersion, string fileContents, bool includeInterop = false)
    {
        fileContents = fileContents.Replace("###PROJECT_NAME###", projectName);
        fileContents = fileContents.Replace("###PROJECT_DESCRIPTION###", projectDescription);
        fileContents = fileContents.Replace("###PROJECT_VERSION###", projectVersion);

        if (_fromSource && includeInterop)
        {
            fileContents = fileContents.Replace("###NOVELRT_ENGINE_LIB###", "Engine\nInterop");
            fileContents = fileContents.Replace("###NOVELRT_ENGINE_SOURCE_CBP###", $"copy_build_products(App" +
                "\n\tDEPENDENCY Resources" +
                $"\n\tTARGET_LOCATION $<TARGET_FILE_DIR:App>/Resources" +
                "\n\n\tDEPENDENCY Engine" +
                $"TARGET_LOCATION $<TARGET_FILE_DIR:App>)" +
                "\n\n\tDEPENDENCY Interop" +
                $"TARGET_LOCATION $<TARGET_FILE_DIR:App>)");
        }
        else if (_fromSource)
        {
            fileContents = fileContents.Replace("###NOVELRT_ENGINE_LIB###", "Engine");
            fileContents = fileContents.Replace("###NOVELRT_ENGINE_SOURCE_CBP###", $"copy_build_products(App" +
                "\n\tDEPENDENCY Resources" +
                $"\n\tTARGET_LOCATION $<TARGET_FILE_DIR:App>/Resources" +
                "\n\n\tDEPENDENCY Engine" +
                $"\n\tTARGET_LOCATION $<TARGET_FILE_DIR:App>)");
            fileContents = fileContents.Replace("###NOVELRT_ENGINE_SRC_DEPENDENCIES###", $"add_dependencies(App Resources)\n");
        }
        else if (includeInterop)
        {
            fileContents = fileContents.Replace("###NOVELRT_ENGINE_LIB###", "NovelRT::Engine\nNovelRT::Interop");
            fileContents = fileContents.Replace("###NOVELRT_ENGINE_SOURCE_CBP###", "");
        }
        else
        {
            fileContents = fileContents.Replace("###NOVELRT_ENGINE_LIB###", "NovelRT::Engine");
            fileContents = fileContents.Replace("###NOVELRT_ENGINE_SOURCE_CBP###", "");
        }

        //fileContents = fileContents.Replace("###NOVELRT_VERSION###", novelrtVersion);
        return fileContents;
    }

    private static async Task CopyDirectoryAsync(string sourceDir, string destinationDir, bool recursive)
    {
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
                SdkLog.Debug($"Copied {copied.FullName}");
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
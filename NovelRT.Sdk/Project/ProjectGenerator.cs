using static NovelRT.Sdk.Globals;

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
    
    public static async Task<string> GenerateAsync(string newProjectPath, string novelrtPath)
    {
        SdkLog.Information($"Creating project...");
        //Generate the template files path
        var templateFilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TemplateFiles");
        var cmakeTemplatePath = Path.Combine(templateFilesPath, "CMakeTemplate");

        SdkLog.Debug($"Attempting to copy project template to {newProjectPath}.");
        //Copy directories to project folder
        await CopyDirectoryAsync(cmakeTemplatePath, newProjectPath, true);

        //Generate project metadata
        var projectName = new DirectoryInfo(newProjectPath).Name;
        var projectDescription = $"{projectName} app";
        var projectVersionString = "0.0.1";
        //var novelrtVersionString = novelrtVersion.ToString(3);

        //Delete the placeholder files
        await DeletePlaceholderFilesWithDirectoryLoggingAsync(new DirectoryInfo(newProjectPath), projectName);

        SdkLog.Information("Applying project metadata...");
        //Overwrite project metadata in CMakeLists
        await OverwriteCMakeTemplateVariableDataAsync(new DirectoryInfo(newProjectPath), projectName!, newProjectPath,
            projectDescription, projectVersionString, novelrtPath);

        SdkLog.Information("Generating conanfile.py...");
        //Generate Conanfile.py for project
        var conanfilePath = newProjectPath + "/conanfile.py";
        File.Copy(templateFilesPath + "/conanfile.py", conanfilePath);

        var conanfileContents = await File.ReadAllTextAsync(conanfilePath);
        conanfileContents = await ApplyProjectContextToFile(projectName!, projectDescription, projectVersionString, novelrtPath, conanfileContents);
        await File.WriteAllTextAsync(conanfilePath, conanfileContents);

        SdkLog.Information("Copying prebuilt resources...");
        //Assume not source build, so copy the resources over
        var engineBinPath = Path.Combine(novelrtPath, "bin");
        var resourcesPath = Path.Combine(engineBinPath, "Resources");
        await CopyDirectoryAsync(resourcesPath, Path.Combine(newProjectPath, "Resources"), true, true);

        //Copy dependencies from pre-built binaries for macOS/Windows
        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
        {
            SdkLog.Information("Copying dependencies...");
            await CopyDirectoryAsync(engineBinPath, Path.Combine(newProjectPath, "deps"), false);
        }
        
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
                SdkLog.Debug($"Generating {placeholderFile.DirectoryName!.Replace("PROJECT_NAME", projectName)}");
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

        var cmakeFiles = rootDir.GetFiles("*CMakeLists.txt");
        
        
        foreach (var cmakeFile in cmakeFiles)
        {
            SdkLog.Debug($"Generating {cmakeFile}");

            string fileContents = await File.ReadAllTextAsync(cmakeFile.FullName);

            fileContents = await ApplyProjectContextToFile(projectName, projectDescription, projectVersion, novelrtPath, fileContents, cmakeFile);

            await File.WriteAllTextAsync(cmakeFile.FullName, fileContents);
        }
    }

    private static async Task<string> ApplyProjectContextToFile(string projectName, string projectDescription, string projectVersion,
        string novelrtPath, string fileContents, FileInfo cmakeFile = null, bool includeInterop = false)
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
            fileContents = fileContents.Replace("###NOVELRT_ENGINE_SOURCE_CBP###",
                "set_target_properties(NovelRT::Engine PROPERTIES" +
                "\n\tMAP_IMPORTED_CONFIG_RELEASE MinSizeRel" +
                "\n\tMAP_IMPORTED_CONFIG_DEBUG RelWithDebInfo)" +
                "\nset_target_properties(NovelRT::Interop PROPERTIES" +
                "\n\tMAP_IMPORTED_CONFIG_RELEASE MinSizeRel" +
                "\n\tMAP_IMPORTED_CONFIG_DEBUG RelWithDebInfo" +
                "\n)" +
                "\ncopy_directory_raw(App \"${GAME_RESOURCES}\")" +
                "\nif (WIN32)" +
                "\n\tcopy_directory_raw(App \"${CMAKE_SOURCE_DIR}/deps\")" +
                "\nendif()");
        }
        else
        {
            fileContents = fileContents.Replace("###NOVELRT_ENGINE_LIB###", "NovelRT::Engine");
            fileContents = fileContents.Replace("###NOVELRT_ENGINE_SOURCE_CBP###",
                "set_target_properties(NovelRT::Engine PROPERTIES" +
                "\n\tMAP_IMPORTED_CONFIG_RELEASE MinSizeRel" +
                "\n\tMAP_IMPORTED_CONFIG_DEBUG RelWithDebInfo" +
                "\n)" +
                "\ncopy_directory_raw(App \"${GAME_RESOURCES}\")" +
                "\nif (WIN32)" +
                "\n\tcopy_directory_raw(App \"${CMAKE_SOURCE_DIR}/deps\")" +
                "\nendif()");
        }

        if (_fromSource)
        {
            var path = novelrtPath.Replace("\\", "/");
            var includePath = Path.Combine(path, "include").Replace("\\", "/");
            string cmakePath = cmakeFile.FullName.Replace(cmakeFile.Name, "build/engine").Replace("\\", "/");
            fileContents = fileContents.Replace($"###NOVELRT_ENGINE_SUBDIR###",
                $"\ninclude_directories(\"{includePath}\")" +
                $"\nadd_subdirectory(\"{path}\" \"{cmakePath}\")" +
                $"\nset_target_properties(Engine PROPERTIES" +
                $"\n\tMAP_IMPORTED_CONFIG_DEBUG RelWithDebInfo)");
        }
        else
        {
            fileContents = fileContents.Replace($"###NOVELRT_ENGINE_SUBDIR###", $""); 
            fileContents = fileContents.Replace($"###NOVELRT_FIND_PACKAGE###", "find_package(BZip2 REQUIRED)" +
                "\nfind_package(flac REQUIRED)" +
                "\nfind_package(Freetype REQUIRED)" +
                "\nfind_package(glfw3 REQUIRED)" +
                "\nfind_package(glm REQUIRED)" +
                "\nfind_package(Microsoft.GSL REQUIRED)" +
                "\nfind_package(Ogg REQUIRED)" +
                "\nfind_package(OpenAL REQUIRED)" +
                "\nfind_package(Opus REQUIRED)" +
                "\nfind_package(PNG REQUIRED)" +
                "\nfind_package(SndFile REQUIRED)" +
                "\nfind_package(spdlog REQUIRED)" +
                "\nfind_package(TBB REQUIRED)" +
                "\nfind_package(Vorbis REQUIRED)" +
                "\nfind_package(Vulkan REQUIRED)" +
                $"\ninclude({novelrtPath}/lib/NovelRT.cmake)");
        }

        //fileContents = fileContents.Replace("###NOVELRT_VERSION###", novelrtVersion);
        return fileContents;
    }

    private static async Task CopyDirectoryAsync(string sourceDir, string destinationDir, bool recursive, bool overwrite = false)
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
            try
            {
                var copied = file.CopyTo(targetFilePath, overwrite);
                SdkLog.Debug($"Copied {copied.FullName}");
            }
            catch (Exception e)
            {
                SdkLog.Error("Could not copy file to target path properly!");
                SdkLog.Error(e.Message);
                SdkLog.Debug(e.StackTrace);
                Environment.Exit(-1);
            }
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
using Serilog;

namespace NovelRT.Sdk;

public class Publisher
{
    private static ILogger? _logger;
   public static async Task PublishAsync(string projectDirectory, string outputDirectory, ILogger logger)
   {
      _logger = logger;

      string tempBuildDirectory = Path.Combine(projectDirectory, "PublishOutput");

      if (Directory.Exists(tempBuildDirectory))
      {
         Directory.Delete(tempBuildDirectory, true);
      }
      
      if (Directory.Exists(outputDirectory) && (Directory.GetFiles(outputDirectory).Length > 0 || Directory.GetDirectories(outputDirectory).Length > 0))
      {
         throw new IOException("The publish output directory is not empty.");
      }
      
      //await ProjectGenerator.ConfigureAsync(projectDirectory, tempBuildDirectory, BuildType.Release);

      await ProjectBuilder.BuildAsync(tempBuildDirectory);

      Directory.Move(tempBuildDirectory, outputDirectory);
      Directory.Delete(tempBuildDirectory, true);
   }
}
namespace NovelRT.Sdk;

public class Publisher
{
   public static async Task PublishAsync(string projectDirectory, string outputDirectory)
   {
      string tempBuildDirectory = Path.Combine(projectDirectory, "PublishOutput");

      if (Directory.Exists(tempBuildDirectory))
      {
         Directory.Delete(tempBuildDirectory, true);
      }

      Directory.CreateDirectory(tempBuildDirectory);

      await ProjectBuilder.BuildAsync(tempBuildDirectory, BuildType.Release);

      if (Directory.Exists(outputDirectory) && (Directory.GetFiles(outputDirectory).Length > 0 || Directory.GetDirectories(outputDirectory).Length > 0))
      {
         throw new IOException("The publish output directory is not empty.");
      }
      
      Directory.Move(tempBuildDirectory, outputDirectory);
   }
}
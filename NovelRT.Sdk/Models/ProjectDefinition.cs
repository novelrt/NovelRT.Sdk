namespace NovelRT.Sdk.Models
{
    public class ProjectDefinition
    {
        public string BuildApp { get; set; }
        
        public string BuildAppArgs { get; set; }

        public string DependencyProfile { get; set; }

        public string EngineLocation { get; set; }

        public string LastBuildConfiguration { get; set; }

        public string Name { get; set; }

        public string OutputDirectory { get; set; }
        
        public string ProjectLocation { get; set; }

        public string Version { get; set; }
    }
}

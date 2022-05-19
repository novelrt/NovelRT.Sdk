namespace NovelRT.Sdk.Models
{
    public class Project
    {
        public string Name { get; set; }

        public string Version { get; set; }

        public string EngineLocation { get; set; }

        public string ProjectLocation { get; set; }
        
        public string BuildApp { get; set; }
        
        public string BuildAppArgs { get; set; }

        public string LastBuildConfiguration { get; set; }

        public string DependencyProfile { get; set; }
    }
}

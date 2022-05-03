using NovelRT.Sdk.Models;
using static NovelRT.Sdk.Globals;
using RestSharp;
using System.Text.Json;
using System.IO.Compression;

namespace NovelRT.Sdk
{
    public static class EngineSelector
    {
        private static readonly string _releasesUrl = @"https://api.github.com/repos/novelrt/NovelRT/releases";

        private static RestClient _client = new RestClient()
            .AddDefaultHeader(KnownHeaders.Accept, "application/vnd.github.v3+json");

        public static async Task<string> SelectEngineVersion()
        {
            int inc = 1;
            Dictionary<int, Release> releases = new Dictionary<int, Release>();
            SdkLog.Information("Getting available NovelRT releases...\n");
            var results = await GetReleases();
            int choice = 0;

            foreach (Release release in results)
            {
                SdkLog.Information($"{inc}. {release.tag_name}");
                releases.Add(inc, release);
                inc++;
            }

            while (choice == 0)
            {
                SdkLog.Information("Select a version and press enter (Q to quit): ");
                string selection = Console.ReadLine();
                if (!int.TryParse(selection, out choice) || choice > releases.Count)
                {
                    if (selection.Contains('Q') || selection.Contains('q'))
                    {
                        SdkLog.Information("Exiting...");
                        Environment.Exit(0);
                    }
                    SdkLog.Error("Invalid selection - please try again.\n");
                    choice = 0;

                    foreach (var r in releases)
                    {
                        SdkLog.Information($"{r.Key}. {r.Value.tag_name}");
                    }
                }
            }

            var selected = releases[choice];
            var location = "";
            if (!await CheckIfVersionDownloaded(selected.tag_name))
            {
                location = await DownloadRelease(await DetermineReleaseForPlatform(selected.assets, Enums.Platform.Win32));
                if (!string.IsNullOrEmpty(location))
                {
                    return await ExtractRelease(selected.tag_name, location);
                }
                else
                {
                    SdkLog.Error("Something went wrong when downloading the release!");
                    return "";
                }
            }
            return "";
        }

        public static async Task<int> ListFoundVersions()
        {
            int inc = 1;
            SdkLog.Information("List of available NovelRT versions:\n");

            var defaultPath = Path.GetFullPath($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/NovelRT/Engine");
            if (Directory.Exists(defaultPath))
            {
                var dirs = Directory.GetDirectories(defaultPath, "*", SearchOption.TopDirectoryOnly);

                foreach (var d in dirs)
                {
                    var f = new FileInfo(d);
                    SdkLog.Information($"{inc}. {f.Name}");
                }

                SdkLog.Information("");

                
            }
            else
            {
                SdkLog.Information("None are locally available at this time.");    
            }
            
            return 0;
        }

        private static async Task<bool> CheckIfVersionDownloaded(string tag)
        {
            var defaultPath = Path.GetFullPath($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/NovelRT/Engine/{tag}");
            SdkLog.Debug($"Checking if NovelRT {tag} exists locally...");
            if (!Directory.Exists(defaultPath))
            {
                SdkLog.Debug($"NovelRT {tag} does not exist at {defaultPath}");
                return false;
            }
            SdkLog.Debug($"NovelRT {tag} exists at {defaultPath}");
            return true;
        }
        private static async Task<ReleaseAsset> DetermineReleaseForPlatform(List<ReleaseAsset> assets, Enums.Platform platform)
        {
            string os = "";

            switch (platform)
            {
                case Enums.Platform.Win32:
                    os = "Windows";
                    break;
                case Enums.Platform.Linux:
                    os = "Ubuntu";
                    break;
                case Enums.Platform.macOS:
                    os = "macOS";
                    break;
                default:
                    throw new NotSupportedException("The target platform could not be determined!");
            }

            foreach (ReleaseAsset asset in assets)
            {
                if (asset.name.Contains(os))
                {
                    return asset;
                }
            }

            throw new NotSupportedException("No release supporting the target platform was found!");
        }

        private static async Task<List<Release>> GetReleases() 
        {
            var request = new RestRequest(_releasesUrl);
            List<Release> response = new List<Release>();
            SdkLog.Debug("Sending request to GitHub...");

            try
            {
                var r = await _client.GetAsync<Release[]>(request);
                SdkLog.Debug($"Received response from GitHub! Number of results: {r.Length}");
                if (r != null)
                    return r.ToList();
            }
            catch (Exception e)
            {
                SdkLog.Error("Error when getting official releases!");
                SdkLog.Error($"{e.Data}");
                SdkLog.Debug($"{e.StackTrace}");
            }
            return response;
        }

        private static async Task<Release> GetSpecificRelease(int id)
        {
            var request = new RestRequest($"{_releasesUrl}/{id}");
            Release response = new Release();

            try
            {
                JsonSerializerOptions _options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var re = await _client.GetAsync(request);
                var r = JsonSerializer.Deserialize<Release>(re.Content, _options);
                if (r != null)
                    return r;
            }
            catch (Exception e)
            {
                SdkLog.Error("Error when getting official releases!");
                if (Globals.Verbosity.MinimumLevel == Serilog.Events.LogEventLevel.Debug)
                {
                    SdkLog.Debug($"{e.Data}", VerboseMessageTemplate);
                    SdkLog.Debug($"{e.StackTrace}", VerboseMessageTemplate);
                }
            }
            return response;
        }

        private static async Task<string> DownloadRelease(ReleaseAsset asset)
        {
            var request = new RestRequest(asset.browser_download_url);
            try
            {
                var tempPath = Environment.GetFolderPath(Environment.SpecialFolder.InternetCache);
                var filePath = Path.Combine(tempPath, asset.name);
                SdkLog.Debug($"Attempting to download release {asset.name} at {tempPath}");
                
                var download = await _client.DownloadStreamAsync(request);
                var temp = File.Create(filePath);
                await download.CopyToAsync(temp);
                temp.Close();
                SdkLog.Debug("Download successful.");
                return filePath;
            }
            catch (Exception e)
            {
                SdkLog.Error("Unable to download release!");
                SdkLog.Error($"{e.Data}");
                SdkLog.Debug($"{e.StackTrace}", VerboseMessageTemplate);
                
                return "";
            }
        }

        private static async Task<string> ExtractRelease(string tag, string filepath, string pathToExtract = "")
        {
            SdkLog.Debug($"Extracting {filepath}...");
            string path;
            if (string.IsNullOrEmpty(pathToExtract))
            {
                path = Path.GetFullPath($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/NovelRT/Engine/{tag}");
            }
            else
            {
                path = pathToExtract;
            }

            try
            {
                if (!Directory.Exists(path))
                {
                    SdkLog.Debug("Directory not found. Creating...");
                    Directory.CreateDirectory(path);
                    SdkLog.Debug("Directory created!");
                }
                SdkLog.Debug("Unzipping contents...");
                ZipFile.ExtractToDirectory(filepath, path);
                SdkLog.Debug("Unzipping successful!");


                return path;
            }
            catch (Exception e)
            {
                SdkLog.Error("Error when extracting release!");
                SdkLog.Error(e.Message);
                SdkLog.Debug(e.StackTrace);
                return "";
            }
        }

    }
}

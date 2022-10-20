using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Update_Handler
{
    [Serializable]
    public struct VersionData
    {
        public string LauncherVersion { get; set; }
        public string GameVersion { get; set; }
        public string LauncherPath { get; set; }
    }
    class Program
    {
        public const string GameTitle = "OpenGL-Voxel-Game";
        public static string mainDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\" + GameTitle;
        public const string DriveVersionLink = "https://drive.google.com/uc?id=1hFp9NRlm6lU3FFn3GQo9nU-2rRLitlBq&usp=download";
        public static string LauncherPath => mainDataPath + "\\Launcher";
        public static string LauncherVersionsDataPath => LauncherPath + "\\VersionData.json";
        public const string DriveLauncherLink = "https://drive.google.com/uc?id=16fO51-JbFIjAeIeNI6OPw_FSQZndS4VV&usp=download";
        public static string downloadedPath = VersionData.LauncherPath.Substring(0, VersionData.LauncherPath.LastIndexOf('\\'));
        public static string parentDirectory = new DirectoryInfo(downloadedPath).Parent.FullName;
        public static bool downloadCompleted = false;
        public static VersionData VersionData
        {
            get
            {
                if (!File.Exists(LauncherVersionsDataPath))
                {
                    var file = File.Create(LauncherVersionsDataPath);
                    file.Close();
                    VersionData data = new VersionData { LauncherVersion = "", GameVersion = "", LauncherPath ="" };
                    string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(LauncherVersionsDataPath, json);
                    return data;

                }

                return JsonSerializer.Deserialize<VersionData>(File.ReadAllText(LauncherVersionsDataPath));
            }
            set
            {
                string json = JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(LauncherVersionsDataPath, json);
            }
        }
        public static async Task<VersionData> GetLatestVersionData()
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(DriveVersionLink);
            string responseBody = await response.Content.ReadAsStringAsync();
            VersionData version = JsonSerializer.Deserialize<VersionData>(responseBody);
            return version;
        }
        public static void Main()
        {
            Console.WriteLine("Started Launcher Update Handler");



            foreach (Process process in Process.GetProcessesByName("Launcher"))
            {
                Console.WriteLine("found process");
                process.Kill();
            }
            WebClient client = new WebClient();
            var address = new Uri(DriveLauncherLink);
            client.DownloadProgressChanged += DownloadProgressChanged;
            client.DownloadFileCompleted += DownloadFinished;

            Console.WriteLine(parentDirectory + "\\Launcher.zip");
            client.DownloadFileAsync(address, parentDirectory + "\\Launcher.zip");
            while (!downloadCompleted) { }
        }

        private static async void DownloadFinished(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (Directory.Exists(downloadedPath))
                Directory.Delete(downloadedPath, true);
            ZipFile.ExtractToDirectory(parentDirectory + "\\Launcher.zip", downloadedPath);

            if (File.Exists(parentDirectory + "\\Launcher.zip"))
                File.Delete(parentDirectory + "\\Launcher.zip");

            Process.Start(downloadedPath + "\\Launcher\\Launcher.exe");
            var data = await GetLatestVersionData();
            VersionData = new VersionData { GameVersion = VersionData.GameVersion, LauncherVersion = data.LauncherVersion, LauncherPath = VersionData.LauncherPath };
            downloadCompleted = true;
            Environment.Exit(0);
        }

        private static void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Console.WriteLine("Download " + e.ProgressPercentage + "% completed");
        }
    }
}

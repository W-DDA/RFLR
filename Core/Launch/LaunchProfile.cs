using System.IO;

namespace MyLauncher.Core.Launch
{
    public class LaunchProfile
    {
        public string JavaBin { get; set; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Java", "jdk-17", "bin", "java.exe");
        public string VersionId { get; set; } = "";
        public string GameDir { get; set; } = "";
        public string AssetsDir { get; set; } = "";
        public string AssetIndex { get; set; } = "";
        public string Classpath { get; set; } = "";
        public string NativesDir { get; set; } = "";
        public int MaxMemoryMb { get; set; } = 4096;
        public int MinMemoryMb { get; set; } = 1024;
        public string Username { get; set; } = "Player";
        public string Uuid { get; set; } = "00000000-0000-0000-0000-000000000000";
        public string AccessToken { get; set; } = "0";
        public bool Isolate { get; set; } = false;
    }
}
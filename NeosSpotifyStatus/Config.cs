using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IniParser;
using IniParser.Model;

namespace NeosSpotifyStatus
{
    internal static class Config
    {
        public const string ConfigBackupFile = "config_old.ini";
        private const string configFile = "config.ini";
        private const string refreshFile = "refreshtoken.txt";

        private static readonly FileIniDataParser parser = new FileIniDataParser();
        private static string refreshToken;

        public static string ClientId { get; }
        public static string ClientSecret { get; }
        public static bool Loaded { get; }
        public static int Port { get; }
        public static int CallbackPort { get; }

        public static string RefreshToken
        {
            get => refreshToken;
            set
            {
                refreshToken = value;
                File.WriteAllText(refreshFile, value);
            }
        }

        static Config()
        {
            if (!File.Exists(configFile))
            {
                ExportDefaultConfig();
                return;
            }

            if (File.Exists(refreshFile))
                RefreshToken = File.ReadAllText(refreshFile);

            var data = parser.ReadFile("config.ini");

            ClientId = data["Spotify"]["ClientID"];
            ClientSecret = data["Spotify"]["ClientSecret"];
            _ = int.TryParse(data["WebserverListen"]["CallbackPort"], out var port1);
            CallbackPort = port1;
            Loaded = int.TryParse(data["WebSocketServer"]["Port"], out var port);
            Port = port;

            Loaded = Loaded && !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);

            if (!Loaded)
                ExportDefaultConfig();
        }

        public static void ExportDefaultConfig()
        {
            if (File.Exists(configFile))
                File.Move(configFile, ConfigBackupFile);

            using var configFileStream = File.Create(configFile);
            var defaultConfigStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("NeosSpotifyStatus." + configFile);
            defaultConfigStream.CopyTo(configFileStream);
        }
    }
}
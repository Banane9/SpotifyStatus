using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions; // Add this for Regex support
using Newtonsoft.Json;
using SpotifyAPI.Web;
using SpotifyStatus.Standalone;

namespace SpotifyStatus
{
    internal static class SpotifyHelper
    {
        private static readonly HttpClient _httpClient = new();

        private static readonly JsonSerializer _jsonSerializer = JsonSerializer.CreateDefault();

        private static readonly Dictionary<string, PlayerSetRepeatRequest.State> _states = new()
        {
            { "track", PlayerSetRepeatRequest.State.Track },
            { "context", PlayerSetRepeatRequest.State.Context },
            { "off", PlayerSetRepeatRequest.State.Off }
        };

        static SpotifyHelper()
        {
            //_httpClient.head
        }

        public static string GetCover(this IPlayableItem playableItem)
        {
            return playableItem switch
            {
                FullTrack track => track.Album.Images[0].Url,
                FullEpisode episode => episode.Images[0].Url,
                _ => null
            };
        }

        public static IEnumerable<SpotifyResource> GetCreators(this IPlayableItem playableItem)
        {
            return playableItem switch
            {
                FullTrack track => track.Artists.Select(artist => new SpotifyResource(artist.Name, artist.ExternalUrls["spotify"])).ToArray(),
                FullEpisode episode => new[] { new SpotifyResource(episode.Show.Name, episode.Show.ExternalUrls["spotify"]) },
                _ => Enumerable.Empty<SpotifyResource>()
            };
        }

        public static int GetDuration(this IPlayableItem playableItem)
        {
            return playableItem switch
            {
                FullTrack track => track.DurationMs,
                FullEpisode episode => episode.DurationMs,
                _ => 100,
            };
        }

        public static SpotifyResource GetGrouping(this IPlayableItem playableItem)
        {
            return playableItem switch
            {
                FullTrack track => new SpotifyResource(track.Album.Name, track.Album.ExternalUrls["spotify"]),
                FullEpisode episode => new SpotifyResource(episode.Show.Name, episode.Show.ExternalUrls["spotify"]),
                _ => null
            };
        }

        public static string GetId(this IPlayableItem playableItem)
        {
            return playableItem switch
            {
                FullTrack track => track.Id,
                FullEpisode episode => episode.Id,
                _ => null
            };
        }

        public static SpotifyResource GetResource(this IPlayableItem playableItem)
        {
            return playableItem switch
            {
                FullTrack track => new SpotifyResource(track.Name, track.ExternalUrls["spotify"]),
                FullEpisode episode => new SpotifyResource(episode.Name, episode.ExternalUrls["spotify"]),
                _ => null,
            };
        }

        public static PlayerSetRepeatRequest.State GetState(string name) => _states[name];

        public static PlayerSetRepeatRequest.State Next(this PlayerSetRepeatRequest.State state)
            => (PlayerSetRepeatRequest.State)(((int)state + 1) % 3);

        public static async void SendCanvasAsync(this IPlayableItem playableItem, Action<SpotifyInfo, string> sendMessage)
        {
            var trackId = playableItem.GetId();
            if (string.IsNullOrEmpty(trackId))
                return;

            try
            {
                string canvasUrl = await GetSpotifyTrackDownloadUrl(trackId);

                if (string.IsNullOrWhiteSpace(canvasUrl))
                {
                    sendMessage(SpotifyInfo.Canvas, "");
                    Console.WriteLine("No canvas for playable found.");
                    return;
                }

                sendMessage(SpotifyInfo.Canvas, canvasUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while getting canvas url");
                Console.WriteLine(ex.ToString());
            }
        }

        private static async Task<string> GetSpotifyTrackDownloadUrl(string trackId)
        {
            string url = $"https://www.canvasdownloader.com/canvas?link=https://open.spotify.com/track/{trackId}";

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode(); // Check for successful HTTP response

                string htmlContent = await response.Content.ReadAsStringAsync();

                // Use Regex to extract the download link from the HTML
                string pattern = @"download-button' onclick=\""window\.open\('(.*?)', '_blank'\)\"";
                Match match = Regex.Match(htmlContent, pattern);

                if (match.Success)
                {
                    return match.Groups[1].Value; // Return the extracted download link
                }
                else
                {
                    Console.WriteLine("Download button not found in HTML.");
                    return null; // Return null if the download button is not found
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching or parsing HTML: {ex.Message}");
                return null; // Return null in case of any error
            }
        }


        public static async void SendLyricsAsync(this IPlayableItem playableItem, Action<SpotifyInfo, string> sendMessage)
        {
            sendMessage(SpotifyInfo.ClearLyrics, "");

            var id = playableItem.GetId();
            if (string.IsNullOrEmpty(id))
                return;

            try
            {
                using var lyricsStream = await _httpClient.GetStreamAsync($"https://spotify-lyrics-api-umber.vercel.app/?trackid={id}");
                using var textReader = new StreamReader(lyricsStream);
                using var jsonTextReader = new JsonTextReader(textReader);

                var lyrics = _jsonSerializer.Deserialize<SongLyrics>(jsonTextReader);

                if (lyrics is null || lyrics.Error)
                {
                    Console.WriteLine("No lyrics for playable found.");
                    return;
                }

                foreach (var line in lyrics.Lines)
                    sendMessage(SpotifyInfo.LyricsLine, line.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while getting lyrics");
                Console.WriteLine(ex.ToString());
            }
        }

        public static int ToUpdateInt(this SpotifyInfo info)
            => info == SpotifyInfo.Clear ? 0 : ((int)Math.Log2((int)info) + 1);
    }
}

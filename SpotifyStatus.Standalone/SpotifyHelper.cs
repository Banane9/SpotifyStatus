using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpotifyAPI.Web;

namespace SpotifyStatus
{
    internal static class SpotifyHelper
    {
        private static readonly Dictionary<string, PlayerSetRepeatRequest.State> states = new Dictionary<string, PlayerSetRepeatRequest.State>()
        {
            { "track", PlayerSetRepeatRequest.State.Track },
            { "context", PlayerSetRepeatRequest.State.Context },
            { "off", PlayerSetRepeatRequest.State.Off }
        };

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

        public static SpotifyResource GetResource(this IPlayableItem playableItem)
        {
            return playableItem switch
            {
                FullTrack track => new SpotifyResource(track.Name, track.ExternalUrls["spotify"]),
                FullEpisode episode => new SpotifyResource(episode.Name, episode.ExternalUrls["spotify"]),
                _ => null,
            };
        }

        public static PlayerSetRepeatRequest.State GetState(string name)
        {
            return states[name];
        }

        public static PlayerSetRepeatRequest.State Next(this PlayerSetRepeatRequest.State state)
        {
            return (PlayerSetRepeatRequest.State)(((int)state + 1) % 3);
        }

        public static int ToUpdateInt(this SpotifyInfo info)
        {
            return info == SpotifyInfo.Clear ? 0 : ((int)Math.Log2((int)info) + 1);
        }

        public static async Task<string> GetSongCanvas(string currentSongId)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"https://spotify-canvas-api-weld.vercel.app/spotify?id={currentSongId}");
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
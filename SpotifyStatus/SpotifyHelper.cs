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
            if (playableItem is FullTrack track)
                return track.Album.Images.FirstOrDefault()?.Url;
            else if (playableItem is FullEpisode episode)
                return episode.Images.FirstOrDefault()?.Url;

            return null;
        }

        public static IEnumerable<SpotifyResource> GetCreators(this IPlayableItem playableItem)
        {
            if (playableItem is FullTrack track)
                return track.Artists.Select(artist => new SpotifyResource(artist.Name, artist.ExternalUrls["spotify"])).ToArray();
            else if (playableItem is FullEpisode episode)
                return new[] { new SpotifyResource(episode.Show.Name, episode.Show.ExternalUrls["spotify"]) };

            return Enumerable.Empty<SpotifyResource>();
        }

        public static int GetDuration(this IPlayableItem playableItem)
        {
            if (playableItem is FullTrack track)
                return track.DurationMs;
            else if (playableItem is FullEpisode episode)
                return episode.DurationMs;

            return 100;
        }

        public static SpotifyResource GetGrouping(this IPlayableItem playableItem)
        {
            if (playableItem is FullTrack track)
                return new SpotifyResource(track.Album.Name, track.Album.ExternalUrls["spotify"]);
            else if (playableItem is FullEpisode episode)
                return new SpotifyResource(episode.Show.Name, episode.Show.ExternalUrls["spotify"]);

            return null;
        }

        public static SpotifyResource GetResource(this IPlayableItem playableItem)
        {
            if (playableItem is FullTrack track)
                return new SpotifyResource(track.Name, track.ExternalUrls["spotify"]);
            else if (playableItem is FullEpisode episode)
                return new SpotifyResource(episode.Name, episode.ExternalUrls["spotify"]);

            return null;
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
            return info == SpotifyInfo.Clear ? 0 : ((int)Math.Log((int)info, 2) + 1);
        }
    }
}
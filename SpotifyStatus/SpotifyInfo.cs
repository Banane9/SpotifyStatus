using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyStatus
{
    /*
     * List of the prefixes/commands sent to Neos via WebSockets:
     * 0 - clear everything
     * 1 - Title
     * 2 - Artist(s)
     * 3 - Cover URL
     * 4 - Album name
     * 5 - Current song position (in ms)
     * 6 - Total song duration (in ms)
     * 7 - Is playing
     * 8 - Repeat state
     * 9 - Shuffle state
     */

    /// <summary>
    /// Ability Flags for what the Client wants to receive. Command Codes for the Status Panel in Neos are 1 + log2(flag).
    /// </summary>
    [Flags]
    internal enum SpotifyInfo
    {
        /// <summary>
        /// Clear status display.
        /// </summary>
        Clear = 0,

        /// <summary>
        /// The playable item.
        /// </summary>
        Playable = 1,

        /// <summary>
        /// Song artist(s) or show creator.
        /// </summary>
        Creator = 1 << 1,

        /// <summary>
        /// Album or podcast cover URI.
        /// </summary>
        Cover = 1 << 2,

        /// <summary>
        /// Album or podcast show title.
        /// </summary>
        Grouping = 1 << 3,

        /// <summary>
        /// Current position in song or podcast in ms.
        /// </summary>
        Progress = 1 << 4,

        /// <summary>
        /// Duration of song or podcast in ms.
        /// </summary>
        Duration = 1 << 5,

        /// <summary>
        /// Playing or Paused.
        /// </summary>
        IsPlaying = 1 << 6,

        /// <summary>
        /// Player repeat state.
        /// </summary>
        RepeatState = 1 << 7,

        /// <summary>
        /// Playlist shuffled or not.
        /// </summary>
        IsShuffled = 1 << 8,

        ResourceUri = 1 << 31
    }
}
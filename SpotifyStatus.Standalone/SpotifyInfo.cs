using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyStatus
{
    /*
     * List of the prefixes/commands sent to Neos via WebSockets:
     * 0  - clear everything
     * 1  - Title
     * 2  - Artist(s) or Show Creator(s)
     * 3  - Cover URL
     * 4  - Album / Podcast name
     * 5  - Current position (in ms)
     * 6  - Total duration (in ms)
     * 7  - Is playing
     * 8  - Repeat state
     * 9  - Shuffle state
     * 10 - Clear queue
     * 11 - Queued Item's Title
     * 12 - Queued Item's Artist(s) or Show Creator
     * 13 - Queued Item's Cover URL
     * 14 - Queued Item's Album / Podcast Name
     * 15 - Queued Item's zero-based Position
     * 16 - Queued Item's Total Duration
     * 17 - Add Queued Item to Display
     * 18 - Queued Item's old and new zero-based position
     * 19 - Queued Items have been updated
     */

    /// <summary>
    /// Ability Flags for what the Client wants to receive. Command Codes for the Status Panel in game are 1 + log2(flag).
    /// </summary>
    [Flags]
    internal enum SpotifyInfo
    {
        /// <summary>
        /// Clear all displayed info.
        /// </summary>
        Clear = 0,

        /// <summary>
        /// The current playable's title.
        /// </summary>
        Playable = 1,

        /// <summary>
        /// The current playable's artist(s) or show creator(s).
        /// </summary>
        Creator = 1 << 1,

        /// <summary>
        /// The current playable's album or podcast cover URI.
        /// </summary>
        Cover = 1 << 2,

        /// <summary>
        /// The current playable's album or podcast show title.
        /// </summary>
        Grouping = 1 << 3,

        /// <summary>
        /// The current playable's current position in ms.
        /// </summary>
        Progress = 1 << 4,

        /// <summary>
        /// The current playable's duration in ms.
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

        /// <summary>
        /// Clear queue (displayed item).
        /// </summary>
        ClearQueue = 1 << 9,

        /// <summary>
        /// A queued playable's title.
        /// </summary>
        QueuedPlayable = 1 << 10,

        /// <summary>
        /// A queued playable's artist(s) or show creator(s).
        /// </summary>
        QueuedCreator = 1 << 11,

        /// <summary>
        /// A queued playable's album or podcast cover URI.
        /// </summary>
        QueuedCover = 1 << 12,

        /// <summary>
        /// A queued playable's album or podcast show title.
        /// </summary>
        QueuedGrouping = 1 << 13,

        /// <summary>
        /// A queued playable's zero-indexed position in the queue.
        /// </summary>
        QueuedPosition = 1 << 14,

        /// <summary>
        /// A queued playable's duration in ms.
        /// </summary>
        QueuedDuration = 1 << 15,

        /// <summary>
        /// Queued playable can be added to display.
        /// </summary>
        QueuedComplete = 1 << 16,

        /// <summary>
        /// A queued playable's old and new position in the queue.
        /// </summary>
        QueuePositionShift = 1 << 17,

        /// <summary>
        /// Queued playables have been updated.
        /// </summary>
        QueueUpdated = 1 << 18,
        
        /// <summary>
        /// Queued playables have been updated.
        /// </summary>
        Canvas = 1 << 19,
    }
}
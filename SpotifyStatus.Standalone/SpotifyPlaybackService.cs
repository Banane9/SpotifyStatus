using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;
using SpotifyAPI.Web;
using System.Text.RegularExpressions;
using System.Globalization;
using SpotifyStatus.Standalone;

namespace SpotifyStatus
{
    internal sealed partial class SpotifyPlaybackService : WebSocketBehavior
    {
        private static readonly Regex SpotifyUriEx = SpotifyUri();

        private readonly List<ChangeTracker> _contextChangeTrackers;
        private CurrentlyPlayingContext _lastPlayingContext;
        private Dictionary<SpotifyResource, int[]> _lastQueue = new();

        public SpotifyPlaybackService()
        {
            _contextChangeTrackers = new List<ChangeTracker>()
            {
                new ChangeTracker(nC => (HandleChangedResource(SpotifyInfo.Playable, nC.Item.GetResource());GetSongCanvas();),
                    (oC, nC) => !oC.Item.GetResource().Equals(nC.Item.GetResource())),

                new ChangeTracker(nC => HandleChangedResources(SpotifyInfo.Creator, nC.Item.GetCreators()),
                    (oC, nC) =>
                    {
                        var oCreators = oC.Item.GetCreators();
                        var nCreators = nC.Item.GetCreators();

                        return oCreators.Count() != nCreators.Count()
                        || oCreators.Except(nCreators).Any()
                        || nCreators.Except(oCreators).Any();
                    }),

                new ChangeTracker(nC => SendMessage(SpotifyInfo.Cover, nC.Item.GetCover()),
                    (oC, nC) => oC.Item.GetCover() != nC.Item.GetCover()),

                new ChangeTracker(nC => HandleChangedResource(SpotifyInfo.Grouping, nC.Item.GetGrouping()),
                    (oC, nC) => !oC.Item.GetGrouping().Equals(nC.Item.GetGrouping())),

                new ChangeTracker(nC => HandleChangedInt(SpotifyInfo.Progress, nC.ProgressMs),
                    (oC, nC) => oC.ProgressMs != nC.ProgressMs),

                new ChangeTracker(nC => HandleChangedInt(SpotifyInfo.Duration, nC.Item.GetDuration()),
                    (oC, nC) => oC.Item.GetDuration() != nC.Item.GetDuration()),

                new ChangeTracker(nC => HandleChangedBool(SpotifyInfo.IsPlaying, nC.IsPlaying),
                    (oC, nC) => oC.IsPlaying != nC.IsPlaying),

                new ChangeTracker(nC => HandleChangedInt(SpotifyInfo.RepeatState, (int)SpotifyHelper.GetState(nC.RepeatState)),
                    (oC, nC) => oC.RepeatState != nC.RepeatState),

                new ChangeTracker(nC => HandleChangedBool(SpotifyInfo.IsShuffled, nC.ShuffleState),
                    (oC, nC) => oC.ShuffleState != nC.ShuffleState),
            };
        }

        protected override void OnClose(CloseEventArgs e)
        {
            SpotifyTracker.PlayingContextUpdated -= SendOutContextUpdates;
            SpotifyTracker.QueueUpdated -= SendOutQueueUpdates;

            Console.WriteLine($"A connection was closed! Reason: {e.Reason}, Code: {e.Code}");
        }

        protected override async void OnMessage(MessageEventArgs e)
        {
            //I'm not going to simply get the playback data on every message received
            //That would be a lot of unneeded requests
            //and I'm already getting ratelimited

            var dividerIndex = e.Data.IndexOf('|');

            int dataStart;
            SpotifyCommand commandCode;

            if (dividerIndex >= 0 && int.TryParse(e.Data[..dividerIndex], out var result))
            {
                commandCode = (SpotifyCommand)result;
                dataStart = dividerIndex + 1;
            }
            else if (int.TryParse(e.Data[..1], out result))
            {
                dataStart = 1;
                commandCode = (SpotifyCommand)result;
            }
            else
            {
                Console.WriteLine("Invalid Command received!");
                Console.WriteLine(e.Data);

                return;
            }

            var updateQueue = false;
            var updatePlayback = true;
            var commandData = e.Data[dataStart..];

            Console.WriteLine($"Command {commandCode} received, data: {commandData}");

            try
            {
                switch (commandCode)
                {
                    case SpotifyCommand.TogglePlayback:
                        if (_lastPlayingContext != null && _lastPlayingContext.IsPlaying)
                            await SpotifyTracker.Spotify.Player.PausePlayback();
                        else
                        {
                            await SpotifyTracker.Spotify.Player.ResumePlayback();
                        }
                        break;

                    case SpotifyCommand.Previous:
                        updateQueue = true;
                        await SpotifyTracker.Spotify.Player.SkipPrevious();
                        break;

                    case SpotifyCommand.Next:
                        updateQueue = true;
                        await SpotifyTracker.Spotify.Player.SkipNext();
                        break;

                    case SpotifyCommand.Refresh:
                        updateQueue = true;
                        _lastQueue.Clear();
                        SendMessage(SpotifyInfo.ClearQueue);

                        _lastPlayingContext = null;
                        break;

                    case SpotifyCommand.CycleRepeat:
                        if (_lastPlayingContext is null)
                            break;

                        var targetState = SpotifyHelper.GetState(_lastPlayingContext.RepeatState).Next();

                        if (int.TryParse(commandData, out var repeatNum))
                            targetState = (PlayerSetRepeatRequest.State)repeatNum;

                        var repeatRequest = new PlayerSetRepeatRequest(targetState);
                        await SpotifyTracker.Spotify.Player.SetRepeat(repeatRequest);
                        break;

                    case SpotifyCommand.ToggleShuffle:
                        var playback2 = await SpotifyTracker.Spotify.Player.GetCurrentPlayback();
                        if (playback2 is null)
                        {
                            Console.WriteLine("No playback detected!");
                            break;
                        }

                        updateQueue = true;
                        var doShuffle = !playback2.ShuffleState;
                        var shuffleRequest = new PlayerShuffleRequest(doShuffle);
                        await SpotifyTracker.Spotify.Player.SetShuffle(shuffleRequest);
                        break;

                    case SpotifyCommand.SeekPlayback:
                        var seekRequest = new PlayerSeekToRequest(int.Parse(commandData));
                        await SpotifyTracker.Spotify.Player.SeekTo(seekRequest);
                        break;

                    case SpotifyCommand.QueueItem:
                        var match = SpotifyUriEx.Match(commandData);
                        if (!match.Success)
                            break;

                        updateQueue = true;
                        updatePlayback = false;

                        var addRequest = new PlayerAddToQueueRequest($"spotify:{match.Groups[2]}:{match.Groups[3]}");
                        await SpotifyTracker.Spotify.Player.AddToQueue(addRequest);

                        // Send parse / add confirmation?
                        // Maybe general toast command
                        break;

                    case SpotifyCommand.Search:
                        updatePlayback = false;

                        if (!await Search(commandData))
                            Console.WriteLine("Invalid Search Request");

                        break;

                    default:
                        Console.WriteLine("Unknown Command");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught: {ex}");
            }

            await Task.Delay(500);

            if (updatePlayback)
                SpotifyTracker.UpdatePlaybackAsync();

            if (updateQueue)
                SpotifyTracker.UpdateQueueAsync();
        }

        protected override void OnOpen()
        {
            SpotifyTracker.PlayingContextUpdated += SendOutContextUpdates;
            SpotifyTracker.QueueUpdated += SendOutQueueUpdates;

            Console.WriteLine("New connection opened, list of sessions:");
            Console.WriteLine($"    {string.Join(Environment.NewLine + "    ", Sessions.ActiveIDs)}");

            SendMessage(SpotifyInfo.ClearQueue);
            Task.Run(() => SpotifyTracker.UpdatePlaybackAsync());
        }

        [GeneratedRegex("(?:spotify:|https?:\\/\\/open\\.spotify\\.com(\\/.*)+?)(episode|track)[:\\/]([0-9A-z]+)")]
        private static partial Regex SpotifyUri();

        private static Queue<T> ToQueue<T>(IEnumerable<T> items) => new(items);

        private void HandleChangedBool(SpotifyInfo info, bool value)
        {
            SendMessage(info, value.ToString(CultureInfo.InvariantCulture));
        }

        private void HandleChangedInt(SpotifyInfo info, int value)
        {
            SendMessage(info, value.ToString(CultureInfo.InvariantCulture));
        }

        private void HandleChangedResource(SpotifyInfo info, SpotifyResource resource)
        {
            SendMessage(info, $"{resource.Uri}|{resource.Name}");
            //sendMessage(info | SpotifyInfo.ResourceUri, resource.Uri);
        }

        private void HandleChangedResources(SpotifyInfo info, IEnumerable<SpotifyResource> resources)
        {
            SendMessage(info, string.Join("\n", resources.Select(res => $"{res.Uri}|{res.Name}")));
            //sendMessage(info | SpotifyInfo.ResourceUri, string.Join(", ", resources.Select(res => res.Uri)));
        }

        private async Task<bool> Search(string request)
        {
            // Should be "limit|types|offset|query"
            var split = request.Split('|');

            if (split.Length != 4)
                return false;

            if (!int.TryParse(split[0], out var limit))
                return false;

            var types = SearchRequest.Types.Track;

            if (!int.TryParse(split[2], out var offset))
                return false;

            var searchRequest = new SearchRequest(types, split[3])
            {
                Limit = limit,
                Offset = offset
            };

            var searchResponse = await SpotifyTracker.Spotify.Search.Item(searchRequest);
            Console.WriteLine($"Search results: {string.Join(", ", searchResponse.Tracks.Items.Select(track => track.Name))}");

            return true;
        }

        private void SendMessage(SpotifyInfo info, string data = "")
        {
            var message = $"{info.ToUpdateInt()}|{data}";

            Console.WriteLine($"Sending {message} to {ID}");
            Task.Run(() => Send(message));
        }

        private void SendOutContextUpdates(CurrentlyPlayingContext newPlayingContext)
        {
            foreach (var changeTracker in _contextChangeTrackers.Where(changeTracker => _lastPlayingContext == null || changeTracker.TestChanged(_lastPlayingContext, newPlayingContext)))
                changeTracker.InvokeEvent(newPlayingContext);

            _lastPlayingContext = newPlayingContext;
        }

        private void SendOutQueueUpdates(List<IPlayableItem> updatedQueue)
        {
            if (updatedQueue.Count == 0)
            {
                _lastQueue.Clear();
                SendMessage(SpotifyInfo.ClearQueue);

                return;
            }

            var newQueue = updatedQueue.Select((playable, idx) => (Resource: playable.GetResource(), Playable: playable, Index: idx))
                .GroupBy(item => item.Resource)
                .ToDictionary(group => group.Key, group =>
                {
                    var indices = group.Select(item => item.Index).Order().ToArray();
                    return (group.First().Playable, Indices: indices, Remaining: ToQueue(indices));
                });

            var updated = false;

            // deleted or moved
            foreach (var lastQueueItem in _lastQueue)
            {
                if (newQueue.TryGetValue(lastQueueItem.Key, out var newItems))
                {
                    for (var i = 0; i < lastQueueItem.Value.Length; ++i)
                    {
                        var oldIndex = lastQueueItem.Value[i];

                        // If no remaining new items, delete it
                        if (newItems.Remaining.Count == 0)
                        {
                            updated = true;
                            SendMessage(SpotifyInfo.ClearQueue, oldIndex.ToString(CultureInfo.InvariantCulture));
                            continue;
                        }

                        // Otherwise move it
                        var newIndex = newItems.Remaining.Dequeue();

                        if (newIndex != oldIndex)
                        {
                            updated = true;
                            SendMessage(SpotifyInfo.QueuePositionShift,
                                $"{oldIndex.ToString(CultureInfo.InvariantCulture)}|{newIndex.ToString(CultureInfo.InvariantCulture)}");
                        }
                    }
                }
                else
                {
                    // If no new item at all, delete it
                    for (var i = 0; i < lastQueueItem.Value.Length; ++i)
                    {
                        updated = true;
                        SendMessage(SpotifyInfo.ClearQueue, lastQueueItem.Value[i].ToString(CultureInfo.InvariantCulture));
                    }
                }
            }

            foreach (var (Playable, _, Remaining) in newQueue.Values)
            {
                // Already covered all new ones by position shifts from old
                if (Remaining.Count == 0)
                    continue;

                updated = true;

                foreach (var newIndex in Remaining)
                    SendQueuedItem(Playable, newIndex);
            }

            if (updated)
                SendMessage(SpotifyInfo.QueueUpdated);

            _lastQueue = newQueue.ToDictionary(group => group.Key, group => group.Value.Indices);
        }

        private void SendQueuedItem(IPlayableItem playable, int index)
        {
            HandleChangedResource(SpotifyInfo.QueuedPlayable, playable.GetResource());
            HandleChangedResources(SpotifyInfo.QueuedCreator, playable.GetCreators());
            SendMessage(SpotifyInfo.QueuedCover, playable.GetCover());
            HandleChangedResource(SpotifyInfo.QueuedGrouping, playable.GetGrouping());
            HandleChangedInt(SpotifyInfo.QueuedPosition, index);

            SendMessage(SpotifyInfo.QueuedComplete);
        }

        private sealed class ChangeTracker
        {
            public Action<CurrentlyPlayingContext> InvokeEvent { get; }

            public Func<CurrentlyPlayingContext, CurrentlyPlayingContext, bool> TestChanged { get; }

            public ChangeTracker(Action<CurrentlyPlayingContext> invokeEvent, Func<CurrentlyPlayingContext, CurrentlyPlayingContext, bool> testChanged)
            {
                InvokeEvent = invokeEvent;
                TestChanged = testChanged;
            }
        }
    }
}
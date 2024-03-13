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

namespace SpotifyStatus
{
    internal class SpotifyPlaybackService : WebSocketBehavior
    {
        private static readonly Regex spotifyUriEx = new Regex(@"(?:spotify:|https?:\/\/open\.spotify\.com\/)(episode|track)[:\/]([0-9A-z]+)");

        private readonly List<ChangeTracker> changeTrackers;
        private CurrentlyPlayingContext lastPlayingContext;

        public SpotifyPlaybackService()
        {
            changeTrackers = new List<ChangeTracker>()
            {
                new ChangeTracker(nC => handleChangedResource(SpotifyInfo.Playable, nC.Item.GetResource()),
                    (oC, nC) => !oC.Item.GetResource().Equals(nC.Item.GetResource())),

                new ChangeTracker(nC => handleChangedResources(SpotifyInfo.Creator, nC.Item.GetCreators()),
                    (oC, nC) =>
                    {
                        var oCreators = oC.Item.GetCreators();
                        var nCreators = nC.Item.GetCreators();

                        return oCreators.Count() != nCreators.Count()
                        || oCreators.Except(nCreators).Any()
                        || nCreators.Except(oCreators).Any();
                    }),

                new ChangeTracker(nC => sendMessage(SpotifyInfo.Cover, nC.Item.GetCover()),
                    (oC, nC) => oC.Item.GetCover() != nC.Item.GetCover()),

                new ChangeTracker(nC => handleChangedResource(SpotifyInfo.Grouping, nC.Item.GetGrouping()),
                    (oC, nC) => !oC.Item.GetGrouping().Equals(nC.Item.GetGrouping())),

                new ChangeTracker(nC => handleChangedInt(SpotifyInfo.Progress, nC.ProgressMs),
                    (oC, nC) => oC.ProgressMs != nC.ProgressMs),

                new ChangeTracker(nC => handleChangedInt(SpotifyInfo.Duration, nC.Item.GetDuration()),
                    (oC, nC) => oC.Item.GetDuration() != nC.Item.GetDuration()),

                new ChangeTracker(nC => handleChangedBool(SpotifyInfo.IsPlaying, nC.IsPlaying),
                    (oC, nC) => oC.IsPlaying != nC.IsPlaying),

                new ChangeTracker(nC => handleChangedInt(SpotifyInfo.RepeatState, (int)SpotifyHelper.GetState(nC.RepeatState)),
                    (oC, nC) => oC.RepeatState != nC.RepeatState),

                new ChangeTracker(nC => handleChangedBool(SpotifyInfo.IsShuffled, nC.ShuffleState),
                    (oC, nC) => oC.ShuffleState != nC.ShuffleState),
            };
        }

        protected override void OnClose(CloseEventArgs e)
        {
            SpotifyTracker.ContextUpdated -= sendOutUpdates;

            Console.WriteLine($"A connection was closed! Reason: {e.Reason}, Code: {e.Code}");
        }

        protected override async void OnMessage(MessageEventArgs e)
        {
            var commandCode = int.Parse(e.Data[0].ToString());
            var commandData = e.Data.Substring(1);
            Console.WriteLine($"Command {commandCode} received, data: {commandData}");
            try
            {
                switch (commandCode)
                {
                    case 0:
                        if (lastPlayingContext != null && lastPlayingContext.IsPlaying)
                        {
                            await SpotifyTracker.Spotify.Player.PausePlayback();
                        }
                        else
                        {
                            await SpotifyTracker.Spotify.Player.ResumePlayback();
                        }
                        break;

                    case 1:
                        await SpotifyTracker.Spotify.Player.SkipPrevious();
                        break;

                    case 2:
                        await SpotifyTracker.Spotify.Player.SkipNext();
                        break;

                    case 3:
                        lastPlayingContext = null;
                        break;

                    case 4:
                        if (lastPlayingContext != null)
                        {
                            var targetState = SpotifyHelper.GetState(lastPlayingContext.RepeatState).Next();

                            if (int.TryParse(commandData, out var repeatNum))
                                targetState = (PlayerSetRepeatRequest.State)repeatNum;

                            var repeatRequest = new PlayerSetRepeatRequest(targetState);
                            await SpotifyTracker.Spotify.Player.SetRepeat(repeatRequest);
                        }
                        break;

                    case 5:
                        var playback2 = await SpotifyTracker.Spotify.Player.GetCurrentPlayback();
                        if (playback2 == null)
                        {
                            Console.WriteLine("No playback detected!");
                            break;
                        }
                        bool doShuffle;
                        if (playback2.ShuffleState)
                        {
                            doShuffle = false;
                        }
                        else
                        {
                            doShuffle = true;
                        }
                        var shuffleRequest = new PlayerShuffleRequest(doShuffle);
                        await SpotifyTracker.Spotify.Player.SetShuffle(shuffleRequest);
                        break;

                    case 6:
                        var seekRequest = new PlayerSeekToRequest(int.Parse(commandData));
                        await SpotifyTracker.Spotify.Player.SeekTo(seekRequest);
                        break;

                    case 7:
                        var match = spotifyUriEx.Match(commandData);
                        if (!match.Success)
                            break;

                        var addRequest = new PlayerAddToQueueRequest($"spotify:{match.Groups[1]}:{match.Groups[2]}");
                        await SpotifyTracker.Spotify.Player.AddToQueue(addRequest);
                        // Send parse / add confirmation?
                        // Maybe general toast command
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught: {ex}");
            }

            await Task.Delay(500);

            SpotifyTracker.Update();
        }

        protected override void OnOpen()
        {
            SpotifyTracker.ContextUpdated += sendOutUpdates;

            Console.WriteLine("New connection opened, list of sessions:");
            Console.WriteLine($"    {string.Join(Environment.NewLine + "    ", Sessions.ActiveIDs)}");

            Task.Run(() => SpotifyTracker.Update());
        }

        private void handleChangedBool(SpotifyInfo info, bool value)
        {
            sendMessage(info, value.ToString(CultureInfo.InvariantCulture));
        }

        private void handleChangedInt(SpotifyInfo info, int value)
        {
            sendMessage(info, value.ToString(CultureInfo.InvariantCulture));
        }

        private void handleChangedResource(SpotifyInfo info, SpotifyResource resource)
        {
            sendMessage(info, $"{resource.Uri}|{resource.Name}");
            //sendMessage(info | SpotifyInfo.ResourceUri, resource.Uri);
        }

        private void handleChangedResources(SpotifyInfo info, IEnumerable<SpotifyResource> resources)
        {
            sendMessage(info, string.Join("\n", resources.Select(res => $"{res.Uri}|{res.Name}")));
            //sendMessage(info | SpotifyInfo.ResourceUri, string.Join(", ", resources.Select(res => res.Uri)));
        }

        private void sendMessage(SpotifyInfo info, string data)
        {
            Console.WriteLine($"Sending {info.ToUpdateInt()}{data} to {ID}");
            Task.Run(() => Send($"{info.ToUpdateInt()}{data}"));
        }

        private void sendOutUpdates(CurrentlyPlayingContext newPlayingContext)
        {
            foreach (var changeTracker in changeTrackers.Where(changeTracker => lastPlayingContext == null || changeTracker.TestChanged(lastPlayingContext, newPlayingContext)))
                changeTracker.InvokeEvent(newPlayingContext);

            lastPlayingContext = newPlayingContext;
        }

        private class ChangeTracker
        {
            public Action<CurrentlyPlayingContext> InvokeEvent { get; }

            public Func<CurrentlyPlayingContext, CurrentlyPlayingContext, bool> TestChanged { get; }

            public ChangeTracker(Action<CurrentlyPlayingContext> invokeEvent, Func<CurrentlyPlayingContext, CurrentlyPlayingContext, bool> testChanged)
            {
                InvokeEvent = invokeEvent;
                TestChanged = testChanged;
            }
        }

        /*
         * List of the prefixes/commands received from Neos via WebSockets:
         * 0 - Pause/Resume
         * 1 - Previous track
         * 2 - Next track
         * 3 - Re-request info
         * 4 - Repeat status change
         * 5 - Toggle shuffle
         * 6 - Seek to position
         * 7 - Add Item to Queue
        */
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace NeosSpotifyStatus
{
    internal class SpotifyAuthorization
    {
        private static readonly ManualResetEventSlim authorizationReceived = new ManualResetEventSlim();
        private static string code;
        private static EmbedIOAuthServer server;

        public static async Task<string> RequestAuthorization()
        {
            authorizationReceived.Reset();

            server = new EmbedIOAuthServer(new Uri("http://localhost:" + Config.CallbackPort + "/callback"), Config.CallbackPort);
            await server.Start();

            server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
            server.ErrorReceived += OnErrorReceived;

            var request = new LoginRequest(server.BaseUri, Config.ClientId, LoginRequest.ResponseType.Code)
            {
                Scope = new[] { Scopes.UserReadCurrentlyPlaying, Scopes.UserModifyPlaybackState,
                    Scopes.UserReadPlaybackState, Scopes.UserReadPlaybackPosition,
                    Scopes.UserLibraryRead, Scopes.UserReadPrivate }
            };
            BrowserUtil.Open(request.ToUri());

            authorizationReceived.Wait();
            return code;
        }

        private static async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            code = response.Code;
            authorizationReceived.Set();

            await server.Stop();

            Console.WriteLine("Received authorization callback, stopped server.");
        }

        private static async Task OnErrorReceived(object sender, string error, string state)
        {
            await server.Stop();

            Console.WriteLine($"Aborting authorization, error received: {error}");
        }
    }
}
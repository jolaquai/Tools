using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks.Sources;

using MetaBrainz.MusicBrainz;

using Newtonsoft.Json;

using SpotifyAPI.Web;

namespace SpotifyPlaylistThingy;

public static class Program
{
    const string clientId = "1104f0d3d5bb48369afbe540fa48902c";
    const string clientSecret = "250925458a9d4b59a046857f6fb47197";
    const string userId = "debedenhasen17";

    private static readonly OAuthClient _oauthClient = new OAuthClient();
    private static SpotifyAuthState spotifyTokens = new SpotifyAuthState();
    private static PKCEAuthenticator authenticator;
    private static SpotifyClient spotifyClient;
    private static string refreshToken;

    public static async Task Main()
    {
        if (File.Exists("auth.json")
            && JsonConvert.DeserializeObject<SpotifyAuthState>(File.ReadAllText("auth.json")) is SpotifyAuthState tokens)
        {
            spotifyTokens = tokens;
            authenticator = new PKCEAuthenticator(clientId, spotifyTokens.TokenResponse);
            var config = SpotifyClientConfig
                .CreateDefault()
                .WithAuthenticator(authenticator);
            spotifyClient = new SpotifyClient(config);

            Console.WriteLine("Authorization refreshed from found auth.json file.");
            Console.WriteLine();
        }
        else
        {
            var listener = new HttpListener();
            var callbackUri = "http://localhost:34897/kliqxSpotifyPlaylistClient";
            var callback = new Uri(callbackUri);
            listener.Prefixes.Add(callbackUri + '/');

            Console.WriteLine("""
            Opened HttpListener on port '34897'
            Serving the following prefix:
            """);
            Array.ForEach(listener.Prefixes.Select(p => $"    {p}").ToArray(), Console.WriteLine);
            listener.Start();

            // Prepare
            var codes = PKCEUtil.GenerateCodes();
            var loginRequest = new LoginRequest(
                callback,
                clientId,
                LoginRequest.ResponseType.Code
            )
            {
                CodeChallengeMethod = "S256",
                CodeChallenge = codes.challenge,
                Scope =
                [
                    Scopes.PlaylistModifyPrivate,
                    Scopes.PlaylistModifyPublic,
                    Scopes.PlaylistReadPrivate,
                    Scopes.PlaylistReadCollaborative,
                    Scopes.UserLibraryModify,
                    Scopes.UserLibraryRead
                ]
            };
            var uri = loginRequest.ToUri();
            Process.Start(new ProcessStartInfo()
            {
                FileName = uri.ToString(),
                UseShellExecute = true
            });

            // Wait for the auth flow to complete
            // TODO: async + make the listener listen persistently in a Task + use events
            await ProcessContext(listener.GetContext(), callback, codes.verifier);
            if (spotifyClient is not null)
            {
                listener.Stop();
            }
            else
            {
                Console.WriteLine("Authorization failed, try again?");
            }

            Console.WriteLine("Authorization successful.");
            Console.WriteLine();
        }

        authenticator.TokenRefreshed += (sender, token) =>
        {
            spotifyTokens.TokenResponse = token;
            File.WriteAllText("auth.json", JsonConvert.SerializeObject(spotifyTokens));
        };

        const string playlistId = "35ahJinOGEF9hP4498Lfxc";

        var pool = ArrayPool<PlaylistTrack<IPlayableItem>>.Shared;
        using var q = new Query("SpotifyPlaylistThingy", "1.0", "j.laquai@gmx.de");
        Query.DelayBetweenRequests = 1.5;

        var genres = new Dictionary<string, string[]>();

        var interves = new SpotifyPager<PlaylistTrack<IPlayableItem>>(await spotifyClient.Playlists.GetItems(playlistId), spotifyClient);
        await foreach (var interf in interves)
        {
            // Querying MusicBrainz for the genres
            var song = interf.Track as FullTrack;
            var artist = song.Artists[0].Name;
            var title = song.Name;
            var recordings = q.FindRecordings($"artist:\"{artist}\" AND recording:\"{title}\"").AsStream();
            // Should ideally only return one recording
            await foreach (var recording in recordings)
            {
                var genresForRec = recording.Item.Genres?.Select(g => g.Name);
                if (genresForRec is null) continue;

                genres.Add($"{artist} - {title}", genresForRec.ToArray());
                if (recording.Score >= 95) break;
            }
        }

        Debugger.Break();

        Console.WriteLine("End of main reached, persisting auth state...");
        File.WriteAllText("auth.json", JsonConvert.SerializeObject(spotifyTokens));
    }

    private static class SpotifyPagingCollector
    {
        public static async Task<int> Collect<T>(Paging<T> pagingOfT, SpotifyClient client, ArraySegment<T> destination)
        {
            var offset = 0;
            pagingOfT.Items.CopyTo(destination.AsSpan(offset, pagingOfT.Items.Count));
            offset += pagingOfT.Items.Count;
            while (pagingOfT.Next is not null)
            {
                pagingOfT = await client.NextPage(pagingOfT);
                pagingOfT.Items.CopyTo(destination.AsSpan(offset, pagingOfT.Items.Count));
                offset += pagingOfT.Items.Count;
            }
            return offset;
        }
    }
    private class SpotifyPager<T>(Paging<T> pagingOfT, SpotifyClient client)
    {
        private readonly SpotifyClient _client = client;
        private Paging<T> paging = pagingOfT;
        private int ptr;
        private List<T> items = pagingOfT.Items;

        public T Current { get; private set; }
        public SpotifyPager<T> GetAsyncEnumerator() => this;
        public async ValueTask<bool> MoveNextAsync()
        {
            if (items is null)
            {
                if (paging.Next is null)
                {
                    return false;
                }
                else if (paging.Next is not null)
                {
                    paging = await _client.NextPage(paging);
                    items = paging.Items;
                    ptr = 1;
                    Current = items[0];
                    return true;
                }
            }
            else if (ptr < items.Count)
            {
                Current = items[ptr++];
                return true;
            } // reached the end of the current page
            else if (paging.Next is not null)
            {
                paging = await _client.NextPage(paging);
                items = paging.Items;
                ptr = 1;
                Current = items[ptr];
                return true;
            }

            return false;
        }
    }
    private class SpotifyAuthState
    {
        public PKCETokenResponse TokenResponse { get; set; }
    }
    private sealed class IPlayableItemEqualityComparer : IEqualityComparer<IPlayableItem>
    {
        public static IPlayableItemEqualityComparer Instance { get; } = new IPlayableItemEqualityComparer();

        public bool Equals(IPlayableItem? x, IPlayableItem? y)
        {
            switch (x)
            {
                case null:
                {
                    return y is null;
                }
                case FullTrack trackX:
                {
                    return y is FullTrack trackY && trackX.Name == trackY.Name;
                }
                case FullEpisode episodeX:
                {
                    return y is FullEpisode episodeY && episodeX.Name == episodeY.Name;
                }
                case FullShow showX:
                {
                    return y is FullShow showY && showX.Name == showY.Name;
                }
                case SimpleTrack simpleTrackX:
                {
                    return y is SimpleTrack simpleTrackY && simpleTrackX.Name == simpleTrackY.Name;
                }
                case SimpleEpisode simpleEpisodeX:
                {
                    return y is SimpleEpisode simpleEpisodeY && simpleEpisodeX.Name == simpleEpisodeY.Name;
                }
                case SimpleShow simpleShowX:
                {
                    return y is SimpleShow simpleShowY && simpleShowX.Name == simpleShowY.Name;
                }
                default:
                {
                    return false;
                }
            }
        }
        public int GetHashCode([DisallowNull] IPlayableItem obj)
        {
            var hc = new HashCode();
            hc.Add(((dynamic)obj).Name.ToUpperInvariant());
            // hc.Add(((dynamic)obj).Id);
            return hc.ToHashCode();
        }
    }

    private static ushort GetFreePort(Random random = null)
    {
        try
        {
            var props = IPGlobalProperties.GetIPGlobalProperties();
            var listeners = props.GetActiveTcpListeners().Concat(props.GetActiveUdpListeners());
            var ports = listeners.Select(ep => ep.Port).ToArray();

            random ??= new Random();
            var port = random.Next(ushort.MaxValue);
            while (ports.Contains(port))
            {
                port = random.Next(ushort.MaxValue);
            }
            return (ushort)port;
        }
        catch
        {
            return (ushort)(random ?? new Random()).Next(1000, ushort.MaxValue);
        }
    }
    private static async Task ProcessContext(HttpListenerContext context, Uri callback, string verifier)
    {
        var code = context.Request.QueryString["code"];

        var initial = await _oauthClient
            .RequestToken(new PKCETokenRequest(clientId, code, callback, verifier)
        );
        spotifyTokens.TokenResponse = initial;

        authenticator = new PKCEAuthenticator(clientId, initial);

        var config = SpotifyClientConfig
            .CreateDefault()
            .WithAuthenticator(authenticator);
        spotifyClient = new SpotifyClient(config);
        refreshToken = initial.RefreshToken;
        File.WriteAllText("auth.json", JsonConvert.SerializeObject(spotifyTokens));

        var outStream = context.Response.OutputStream;
        outStream.Write(Encoding.Default.GetBytes($$"""
            <html>
                <head>
                    <title>Spotify Playlist Thingy</title>
                    <style>
                        body {
                            font-family: sans-serif;
                        }
                    </style>
                    <script>
                        window.close();
                    </script>
                </head>

                <body>
                    <h1>Spotify Playlist Thingy</h1>
                    <p>Authorization successful, you may now close this tab.</p>
                </body>
            </html>
            """));
        outStream.Close();
    }
}

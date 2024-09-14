using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

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

        const string artistId = "4DWX7u8BV0vZIQSpJQQDWU";
        const string playlistId = "2Rza9epiftqYFSgPZdKrvE";
        const string newPlaylistName = "AB Inverted";

        var artistTracksTask = Task.Run(async () =>
        {
            var artistTracks = new List<FullTrack>();
            var artistsAlbums = (await spotifyClient.Artists.GetAlbums(artistId)).Items
                .Where(album => !album.Name.Contains("live", StringComparison.OrdinalIgnoreCase))
                .Select(album => album.Id);
            foreach (var album in artistsAlbums)
            {
                var fullAlbum = await spotifyClient.Albums.Get(album);
                var trackIds = fullAlbum.Tracks.Items.Select(track => track.Id);
                var request = new TracksRequest(trackIds.ToArray());
                var tracks = await spotifyClient.Tracks.GetSeveral(request);
                artistTracks.AddRange(tracks.Tracks);
            }
            return artistTracks;
        });

        var playlistTracks = (await spotifyClient.Playlists.GetItems(playlistId)).Items
            .Select(item => (FullTrack)item.Track)
            .ToArray();

        var newPlaylist = await spotifyClient.Playlists
            .Create(userId, new PlaylistCreateRequest(newPlaylistName));
        // Add all the tracks by the artist to the new playlist that aren't already in the original playlist
        var artistTracks = await artistTracksTask;
        var tracksToAdd = artistTracks
            .Except(playlistTracks, IPlayableItemEqualityComparer.Instance)
            .Cast<FullTrack>()
            .Where(track => !track.Name.Contains("live", StringComparison.OrdinalIgnoreCase))
            .DistinctBy(track => track.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        foreach (var chunk in tracksToAdd.Chunk(100))
        {
            await spotifyClient.Playlists.AddItems(newPlaylist.Id, new PlaylistAddItemsRequest(chunk.Select(track => track.Uri).ToArray()));
        }

        Console.WriteLine("End of main reached, persisting auth state...");
        File.WriteAllText("auth.json", JsonConvert.SerializeObject(spotifyTokens));
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

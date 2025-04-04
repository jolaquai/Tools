using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

using LaquaiLib.Extensions;
using LaquaiLib.Util;

namespace BlueskyDownloader;

// We have no SynchronizationContext
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

internal class Program
{
    public static DirectoryInfo Resources { get; }

    static Program()
    {
        Resources = new DirectoryInfo(Environment.CurrentDirectory);
        while (!Resources.Name.Equals("BlueskyDownloader"))
        {
            Resources = Resources.Parent;
            if (Resources is null)
                throw new DirectoryNotFoundException("BlueskyDownloader directory not found.");
        }
        Resources = Resources.Directory("Resources");
        if (!Resources.Exists)
        {
            Resources.Create();
        }

        Resources.Refresh();
    }

    public static async Task Main()
    {
        var client = new HttpClient()
        {
            BaseAddress = new Uri(@"https://bsky.social/xrpc/"),
            Timeout = Timeout.InfiniteTimeSpan,
            MaxResponseContentBufferSize = int.MaxValue,
        };
        // Auth
        var previousSession = Resources.File("bsky_session.json");
        BskySession session = null;
        if (previousSession.Exists)
        {
            session = JsonSerializer.Deserialize<BskySession>(await previousSession.ReadAllTextAsync(default), BskySerializerContext.Default.Options);
        }
        // Has the side effect of "rehydrating" a previously saved session that has gone stale and gives it its HttpClient
        session = await BskySession.GetSessionAsync(client, session);
        await using (var fs = previousSession.OpenWrite())
        {
            await session.SaveSessionAsync(fs);
        }

        string[] posts =
        [

        ];
        for (var i = 0; i < posts.Length; i++)
        {
            await session.DownloadPostMediaHttpAsync(posts[i]);
        }
    }
}

#region Models
[JsonSerializable(typeof(BskySession))]
[JsonSerializable(typeof(BskyHandleResolution))]
[JsonSerializable(typeof(BskyPosts))]
[JsonSerializable(typeof(BskyPostView))]
[JsonSerializable(typeof(Author))]
[JsonSerializable(typeof(Record))]
[JsonSerializable(typeof(RecordEmbed))]
[JsonSerializable(typeof(AspectRatio))]
[JsonSerializable(typeof(Video))]
[JsonSerializable(typeof(VideoRef))]
[JsonSerializable(typeof(Facet))]
[JsonSerializable(typeof(Feature))]
[JsonSerializable(typeof(FacetIndex))]
[JsonSerializable(typeof(SelfLabels))]
[JsonSerializable(typeof(LabelValue))]
[JsonSerializable(typeof(Embed))]
[JsonSerializable(typeof(Viewer))]
[JsonSerializable(typeof(Label))]
[JsonSerializable(typeof(AuthorViewer))]
[JsonSerializable(typeof(ImagesEmbed))]
[JsonSerializable(typeof(VideoEmbed))]
[JsonSerializable(typeof(BskyPostView[]))]
[JsonSerializable(typeof(Label[]))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(object[]))]
[JsonSerializable(typeof(Feature[]))]
[JsonSerializable(typeof(Facet[]))]
[JsonSerializable(typeof(LabelValue[]))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(Label[]))]
[JsonSerializable(typeof(LabelValue[]))]
[JsonSerializable(typeof(Facet[]))]
[JsonSerializable(typeof(Feature[]))]
[JsonSerializable(typeof(LabelValue[]))]
[JsonSerializable(typeof(FacetIndex[]))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
public partial class BskySerializerContext : JsonSerializerContext;

#region General
public class BskyHandleResolution
{
    public string Did { get; set; }
}

public class BskyPosts
{
    public BskyPostView[] Posts { get; set; }
}
public class BskyPostView
{
    public string Uri { get; set; }

    public string Cid { get; set; }

    public Author Author { get; set; }

    public Record Record { get; set; }

    public Embed Embed { get; set; }

    public int ReplyCount { get; set; }

    public int RepostCount { get; set; }

    public int LikeCount { get; set; }

    public int QuoteCount { get; set; }

    public DateTime IndexedAt { get; set; }

    public Viewer Viewer { get; set; }

    public Label[] Labels { get; set; }
}

public class Author
{
    [JsonPropertyName("did")]
    public string Did { get; set; }

    [JsonPropertyName("handle")]
    public string Handle { get; set; }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; }

    [JsonPropertyName("avatar")]
    public string Avatar { get; set; }

    [JsonPropertyName("viewer")]
    public AuthorViewer Viewer { get; set; }

    [JsonPropertyName("labels")]
    public object[] Labels { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

public class AuthorViewer
{
    [JsonPropertyName("muted")]
    public bool Muted { get; set; }

    [JsonPropertyName("blockedBy")]
    public bool BlockedBy { get; set; }

    [JsonPropertyName("following")]
    public string Following { get; set; }
}

public class Record
{
    [JsonPropertyName("$type")]
    public string Type { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("embed")]
    public object Embed { get; set; }

    [JsonPropertyName("facets")]
    public Facet[] Facets { get; set; }

    [JsonPropertyName("labels")]
    public SelfLabels Labels { get; set; }

    [JsonPropertyName("langs")]
    public string[] Langs { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }
}

public class RecordEmbed
{
    [JsonPropertyName("$type")]
    public string Type { get; set; }

    [JsonPropertyName("aspectRatio")]
    public AspectRatio AspectRatio { get; set; }

    [JsonPropertyName("video")]
    public Video Video { get; set; }
}

public class Video
{
    [JsonPropertyName("$type")]
    public string Type { get; set; }

    [JsonPropertyName("ref")]
    public VideoRef Ref { get; set; }

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }
}

public class VideoRef
{
    [JsonPropertyName("$link")]
    public string Link { get; set; }
}

public class AspectRatio
{
    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }
}

public class Facet
{
    [JsonPropertyName("features")]
    public Feature[] Features { get; set; }

    [JsonPropertyName("index")]
    public FacetIndex Index { get; set; }
}

public class Feature
{
    [JsonPropertyName("$type")]
    public string Type { get; set; }

    [JsonPropertyName("tag")]
    public string Tag { get; set; }
}

public class FacetIndex
{
    [JsonPropertyName("byteEnd")]
    public int ByteEnd { get; set; }

    [JsonPropertyName("byteStart")]
    public int ByteStart { get; set; }
}

public class SelfLabels
{
    [JsonPropertyName("$type")]
    public string Type { get; set; }

    [JsonPropertyName("values")]
    public LabelValue[] Values { get; set; }
}

public class LabelValue
{
    [JsonPropertyName("val")]
    public string Val { get; set; }
}

public class Embed
{
    [JsonPropertyName("$type")]
    public string Type { get; set; }

    [JsonPropertyName("cid")]
    public string Cid { get; set; }

    [JsonPropertyName("playlist")]
    public string Playlist { get; set; }

    [JsonPropertyName("thumbnail")]
    public string Thumbnail { get; set; }

    [JsonPropertyName("aspectRatio")]
    public AspectRatio AspectRatio { get; set; }
}

public class Viewer
{
    [JsonPropertyName("threadMuted")]
    public bool ThreadMuted { get; set; }

    [JsonPropertyName("embeddingDisabled")]
    public bool EmbeddingDisabled { get; set; }
}

public class Label
{
    [JsonPropertyName("cid")]
    public string Cid { get; set; }

    [JsonPropertyName("cts")]
    public DateTime Cts { get; set; }

    [JsonPropertyName("src")]
    public string Src { get; set; }

    [JsonPropertyName("uri")]
    public string Uri { get; set; }

    [JsonPropertyName("val")]
    public string Val { get; set; }

    [JsonPropertyName("ver")]
    public int? Ver { get; set; }
}
#endregion

#region Embed Type: images
public class ImagesEmbed
{
    [JsonPropertyName("$type")]
    public string Type { get; set; }

    [JsonPropertyName("images")]
    public ImageItem[] Images { get; set; }
}

public class ImageItem
{
    [JsonPropertyName("alt")]
    public string Alt { get; set; }

    [JsonPropertyName("aspectRatio")]
    public AspectRatio AspectRatio { get; set; }

    [JsonPropertyName("image")]
    public ImageData Image { get; set; }
}

public class ImageData
{
    [JsonPropertyName("$type")]
    public string Type { get; set; }

    [JsonPropertyName("ref")]
    public Reference Ref { get; set; }

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }
}

public class Reference
{
    [JsonPropertyName("$link")]
    public string Link { get; set; }
}
#endregion
#region Embed Type: video
public class VideoEmbed
{
    [JsonPropertyName("$type")]
    public string Type { get; set; }

    [JsonPropertyName("aspectRatio")]
    public AspectRatio AspectRatio { get; set; }

    [JsonPropertyName("video")]
    public VideoData Video { get; set; }
}

public class VideoData
{
    [JsonPropertyName("$type")]
    public string Type { get; set; }

    [JsonPropertyName("ref")]
    public Reference Ref { get; set; }

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }
}
#endregion
#endregion

/// <summary>
/// Represents a session for interacting with the Bsky API, managing JWTs and session state.
/// This class is NOT thread-safe. Concurrent use will very likely corrupt state.
/// </summary>
[DebuggerDisplay("AccessJwt ({AccessJwt?.Length.ToString() ?? \"null\"}), RefreshJwt ({RefreshJwt?.Length.ToString() ?? \"null\"}), client = {client?.ToString() ?? \"null\"}")]
public class BskySession
{
    private HttpClient client;

    public string AccessJwt { get; set; }
    public string RefreshJwt { get; set; }

    public static async Task<BskySession> GetSessionAsync(HttpClient client, BskySession previous)
    {
        const string handle = "uwumatic101.bsky.social";
        const string pass = "Uwu.Matic.101";

        if (previous is null)
        {
            using var msg = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(client.BaseAddress, "com.atproto.server.createSession"),
                Content = new StringContent($$"""
                    { "identifier": "{{handle}}", "password": "{{pass}}" }
                    """, new MediaTypeHeaderValue("application/json"))
            };
            using var response = await client.SendAsync(msg);
            var readAsString = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();
            previous = await JsonSerializer.DeserializeAsync<BskySession>(await response.Content.ReadAsStreamAsync(), BskySerializerContext.Default.Options, default);
            previous.client = client;
        }
        else
        {
            previous.client = client;
            await previous.RefreshStateAsync();
        }

        previous.client = client;

        return previous;
    }
    /// <summary>
    /// Called before every use of the session. Do not call manually to guard other instance method calls.
    /// </summary>
    public async Task RefreshStateAsync()
    {
        Debug.Assert(client is not null);

        using var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(client.BaseAddress, "com.atproto.server.refreshSession"),
            Headers =
            {
                { "Authorization", $"Bearer {RefreshJwt}" }
            }
        };
        using var response = await client.SendAsync(msg);
        var readAsString = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        await using var utf8Json = await response.Content.ReadAsStreamAsync();
        var newSession = await JsonSerializer.DeserializeAsync<BskySession>(utf8Json, BskySerializerContext.Default.Options, default) ?? throw new InvalidOperationException("Failed to refresh session.");
        AccessJwt = newSession.AccessJwt;
        RefreshJwt = newSession.RefreshJwt;
    }
    /// <summary>
    /// Serializes the current state into a <see cref="Stream"/>.
    /// </summary>
    public async Task SaveSessionAsync(Stream stream)
    {
        await RefreshStateAsync();
        await JsonSerializer.SerializeAsync(stream, this, BskySerializerContext.Default.Options);
    }

    public async Task<string> ResolveHttpUriToAtUri(string httpUri)
    {
        // Extract key parts from HTTP URI
        var uri = new Uri(httpUri);
        var path = uri.AbsolutePath;

        // Path format should be /profile/{handle}/post/{postId}
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 4 || segments[0] != "profile" || segments[2] != "post")
        {
            throw new ArgumentException("Invalid Bluesky HTTP URI format");
        }

        var handle = segments[1];
        var postId = segments[3];

        // First, resolve the handle to a DID
        var did = await ResolveHandleToDid(handle);
        if (string.IsNullOrEmpty(did))
        {
            throw new Exception($"Failed to resolve handle: {handle}");
        }

        // Construct AT-URI
        return $"at://{did}/app.bsky.feed.post/{postId}";
    }
    public async Task<string> ResolveHandleToDid(string handle)
    {
        await RefreshStateAsync();

        var response = await client.GetAsync($"com.atproto.identity.resolveHandle?handle={handle}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return (await JsonSerializer.DeserializeAsync<BskyHandleResolution>(await response.Content.ReadAsStreamAsync(), BskySerializerContext.Default.Options, default)).Did;
    }
    public async Task DownloadPostMediaHttpAsync(string httpUri) => await DownloadPostMediaATAsync(await ResolveHttpUriToAtUri(httpUri));
    public async Task DownloadPostMediaATAsync(string atUri)
    {
        await RefreshStateAsync();

        // Get the post data using the AT URI
        var requestUri = $"app.bsky.feed.getPosts?uris={atUri}";
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUri)
        {
            Headers = { { "Authorization", $"Bearer {AccessJwt}" } }
        });
        if (!response.IsSuccessStatusCode)
        {
            var readAsString = await response.Content.ReadAsStringAsync();
            Debug.Fail(readAsString);
        }
        response.EnsureSuccessStatusCode();

        // Extract post ID from AT URI for folder naming
        var parts = atUri.Split('/');
        var postId = parts[^1];
        var didFragment = parts[2].Split(':')[^1]; // Extract DID from AT URI

        // Get the resources directory
        var postDir = Directory.CreateDirectory(Path.Combine(Program.Resources.FullName, $"{didFragment}_{postId}"));

        var responseJson = await response.Content.ReadAsStringAsync();
        // Read as JsonDocument, then save into response.json to get the pretty-printed JSON
        using var jsonDocument = JsonDocument.Parse(responseJson);
        await using (var stream = postDir.File("response.json").Create())
        using (var streamWriter = new Utf8JsonWriter(stream, new JsonWriterOptions()
        {
            Indented = true,
            IndentSize = 2,
            IndentCharacter = ' '
        }))
        {
            jsonDocument.WriteTo(streamWriter);
        }

        var mediaCount = 0;

        // Parse the JSON response
        BskyPosts postViews;
        await using (var utf8Json = await response.Content.ReadAsStreamAsync())
        {
            postViews = await JsonSerializer.DeserializeAsync<BskyPosts>(utf8Json, BskySerializerContext.Default.Options, default);
        }

        var post = postViews.Posts[0];
        var sectionElement = jsonDocument.RootElement.GetProperty("posts")[0].GetProperty("record").GetProperty("embed");
        var section = sectionElement.ToString();
        var embedType = sectionElement.GetProperty("$type").GetString();

        // Get the section in the JsonDocument and deserialize it into the appropriate type manually here
        switch (embedType)
        {
            case "app.bsky.embed.video":
            {
                var videoEmbed = JsonSerializer.Deserialize<VideoEmbed>(section, BskySerializerContext.Default.Options);

                var did = post.Author.Did;
                var cid = post.Embed.Cid;
                var ext = videoEmbed.Video.MimeType.AsSpan()[(videoEmbed.Video.MimeType.AsSpan().LastIndexOf('/') + 1)..];
                try
                {
                    await DownloadBlobAsync(did, cid, postDir.File($"{postId}.{ext}").FullName);
                }
                catch (IOException)
                {
                    // Probably some fucked up Content-Type in that blob
                    // Save it as a .bin instead and add a separate .txt file with the fallback file name and mime type
                    var fallbackFileName = $"{postId}.bin";
                    await DownloadBlobAsync(did, cid, postDir.File(fallbackFileName).FullName, videoEmbed.Video.Size);
                    await using var fallbackFile = postDir.File($"{postId}.txt").CreateText();
                    await fallbackFile.WriteAsync($"""
                        Fallback file name: {fallbackFileName};
                        Mime type: {videoEmbed.Video.MimeType}
                        """);
                }
                break;
            }
            case "app.bsky.embed.images":
            {
                var imagesEmbed = JsonSerializer.Deserialize<ImagesEmbed>(section, BskySerializerContext.Default.Options);
                for (var i = 0; i < imagesEmbed.Images.Length; i++)
                {
                    var fullsizeUri = imagesEmbed.Images[i].Image.Ref.Link;
                    await DownloadMediaAsync(fullsizeUri, postDir.File($"image_{i}.jpg").FullName);
                }
                break;
            }
            default:
            {
                Debug.Fail($"Unknown embed type: {embedType}");
                break;
            }
        }
    }

    private async Task DownloadBlobAsync(string did, string cid, string destinationPath, int sizeHint = 0)
    {
        var blobResponse = await client.GetAsync($"com.atproto.sync.getBlob?did={did}&cid={cid}");
        blobResponse.EnsureSuccessStatusCode();
        if (sizeHint <= 0)
        {
            if (blobResponse.Content.Headers.ContentLength is long contentLength and < int.MaxValue)
            {
                // Try to get the size from the response headers if it's not specified explicitly, otherwise fall back to .NET's default
                sizeHint = (int)contentLength;
            }
            else
            {
                sizeHint = 81920;
            }
        }
        using var fileStream = File.Create(destinationPath, sizeHint);
        await blobResponse.Content.CopyToAsync(fileStream);
    }
    private async Task DownloadMediaAsync(string url, string destinationPath)
    {
        var mediaResponse = await client.GetAsync(url);
        mediaResponse.EnsureSuccessStatusCode();

        using var fileStream = File.Create(destinationPath);
        await mediaResponse.Content.CopyToAsync(fileStream);
    }
}

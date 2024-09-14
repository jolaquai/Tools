namespace XmlShit;

using System.Collections.Concurrent;
using System.IO.Compression;
using System.Xml;

internal class ProjectResourcesXmlResolver : XmlUrlResolver
{
    private const string defaultApiVersion = "1.0";
#if false
    private const string defaultApiBaseUrl = "http://sa-iistest2019/ProjectResourcesApi/api/";
    private const string defaultApiToken = "sE6wG9lYSk4JWCABTfOLCID5QQNDbV8y2qXFPGpcZ0HRMZ7TUOW13";
#else
    private const string defaultApiBaseUrl = "https://flowdocx.becksche.de/ProjectResourcesApi/api/";
    private const string defaultApiToken = "RCIikPBJ1LQA0mNM7s2XTKjbGgyJOpCFxHDq5A8U3cTSVl9O6YKW4";
#endif

    private static readonly HttpClient _client = new HttpClient()
    {
        BaseAddress = new Uri(defaultApiBaseUrl),
        MaxResponseContentBufferSize = int.MaxValue,
        Timeout = TimeSpan.FromHours(1),
        DefaultRequestHeaders =
        {
            { "api-version", defaultApiVersion },
            { "api-token", defaultApiToken },
        }
    };

    private readonly string[] dtdFilesToDownload = ["aktuell_jurbook.zip"];

    private readonly DateTime lastDtdsUpdate = DateTime.MinValue;
    private readonly TimeSpan maxCacheDuration = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, Stream> cachedDtdFiles = new ConcurrentDictionary<string, Stream>();

    public async Task UpdateCachedDtdFiles()
    {
        foreach (var downloadFileName in dtdFilesToDownload)
        {
            var requestUri = new Uri($"Resource/Download?projectName=DTDs&fileName={Uri.EscapeDataString(downloadFileName)}", UriKind.Relative);

            using var result = await (await _client.GetAsync(requestUri)).Content.ReadAsStreamAsync();

            if (downloadFileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                using var zipArchive = new ZipArchive(result, ZipArchiveMode.Read);
                foreach (var zipEntry in zipArchive.Entries)
                {
                    var cacheStream = new MemoryStream();
                    await zipEntry.Open().CopyToAsync(cacheStream);
                    cacheStream.Seek(0, SeekOrigin.Begin);

                    var zipEntryKey = NormalizeFilePath(Path.Combine(Path.GetFileNameWithoutExtension(downloadFileName), zipEntry.FullName));

                    cachedDtdFiles.AddOrUpdate(zipEntryKey, cacheStream, (key, oldValue) => cacheStream);
                }
            }
            else
            {
                var cacheStream = new MemoryStream();
                await result.CopyToAsync(destination: cacheStream);
                cacheStream.Seek(0, SeekOrigin.Begin);

                cachedDtdFiles.AddOrUpdate(NormalizeFilePath(downloadFileName), cacheStream, (key, oldValue) => cacheStream);
            }
        }
    }

    public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
    {
        if (DateTime.Now - lastDtdsUpdate > maxCacheDuration)
        {
            Task.Run(() => UpdateCachedDtdFiles()).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        foreach (var downloadFileName in dtdFilesToDownload)
        {
            var dtdDirectory = NormalizeFilePath(Path.GetFileNameWithoutExtension(downloadFileName));

            var jurbookKeyMatch = cachedDtdFiles.Keys.OrderByDescending(k => k.Length).FirstOrDefault(k => (dtdDirectory + "/" + NormalizeFilePath(absoluteUri.AbsolutePath)).Contains(k));

            if (jurbookKeyMatch != null)
            {
                cachedDtdFiles[jurbookKeyMatch].Seek(0, SeekOrigin.Begin);
                return cachedDtdFiles[jurbookKeyMatch];
            }
        }

        try
        {
            if (!File.Exists(absoluteUri.AbsolutePath))
            {
                return null;
            }

            return base.GetEntity(absoluteUri, role, ofObjectToReturn);
        }
        catch
        {
            return null;
        }
    }

    public override async Task<object> GetEntityAsync(Uri absoluteUri, string? role, Type? ofObjectToReturn)
    {
        if (DateTime.Now - lastDtdsUpdate > maxCacheDuration)
        {
            await UpdateCachedDtdFiles();
        }

        foreach (var downloadFileName in dtdFilesToDownload)
        {
            var dtdDirectory = NormalizeFilePath(Path.GetFileNameWithoutExtension(downloadFileName));

            var jurbookKeyMatch = cachedDtdFiles.Keys.OrderByDescending(k => k.Length).FirstOrDefault(k => (dtdDirectory + "/" + NormalizeFilePath(absoluteUri.AbsolutePath)).Contains(k));

            if (jurbookKeyMatch != null)
            {
                cachedDtdFiles[jurbookKeyMatch].Seek(0, SeekOrigin.Begin);
                return cachedDtdFiles[jurbookKeyMatch];
            }
        }

        try
        {
            if (!File.Exists(absoluteUri.AbsolutePath))
            {
                return Task.FromResult((object?)null);
            }

            return await base.GetEntityAsync(absoluteUri, role, ofObjectToReturn);
        }
        catch
        {
            return Task.FromResult((object?)null);
        }
    }

    private string NormalizeFilePath(string input)
    {
        if (char.IsLetter(input[0]) && input[1] == ':')
        {
            input = input.Substring(2);
        }

        input = input.Replace(@"\\nchbfsem\cd-rom\DTD\aktuell_BIT", "", StringComparison.OrdinalIgnoreCase);
        input = input.Replace(@"\\nchbfsem\cd-rom\DTD\aktuell_BITL2", "", StringComparison.OrdinalIgnoreCase);

        input = input.Replace(@"\\nchbfsem\cd-rom\DTD\aktuell_jurBook", "", StringComparison.OrdinalIgnoreCase);

        input = input.Replace(@"\\nchbfsem\cd-rom\DTD\aktuell_jurLaw", "", StringComparison.OrdinalIgnoreCase);

        input = input.ToLower().Replace("\\", "/").TrimStart('/').ToLower();

        return input;
    }
}
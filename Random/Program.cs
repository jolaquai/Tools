using System;
using System.Net.Http.Headers;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

namespace _Random;

public static partial class Program
{
    static Program()
    {
        var services = new ServiceCollection();

        services.AddSingleton<HttpClient>();
        services.AddSingleton<GitHubService>();

        serviceProvider = services.BuildServiceProvider();
    }
    private static ServiceProvider serviceProvider;

    public static async Task Main()
    {
        while (true)
        {
            try
            {
                var svc = serviceProvider.GetRequiredService<GitHubService>();
                var releases = await svc.GetReleasesAsync("dotnet", "sdk");

                foreach (var release in releases.Where(r => r.PublishedAt.Date == DateTime.Today))
                {
                    Console.WriteLine($"Release: {release.Name} ({release.TagName})");
                    Console.WriteLine($"Published at: {release.PublishedAt}");
                    Console.WriteLine($"Release notes: {release.Body}");
                    Console.WriteLine($"URL: {release.HtmlUrl}");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine();
            }

            await Task.Delay(TimeSpan.FromMinutes(10));
        }
    }
}

public class GitHubRelease
{
    public string TagName { get; set; }
    public string Name { get; set; }
    public string Body { get; set; }
    public DateTime PublishedAt { get; set; }
    public string HtmlUrl { get; set; }
}

public class GitHubService
{
    private readonly HttpClient _httpClient;

    public GitHubService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.github.com/");
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DotNetApp-" + Guid.NewGuid().ToString(), "1.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
    }

    private static readonly JsonSerializerOptions _options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    public async Task<GitHubRelease[]> GetReleasesAsync(string owner, string repo)
    {
        var response = await _httpClient.GetAsync($"repos/{owner}/{repo}/releases");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GitHubRelease[]>(content, _options);
    }
}

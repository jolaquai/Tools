using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

namespace _Random;

public static partial class Program
{
    private static readonly DirectoryInfo _resources = new DirectoryInfo(@"E:\PROGRAMMING\Projects\C#\Tools\Random\Resources");

    static Program()
    {
        var services = new ServiceCollection();

        services.AddSingleton<HttpClient>();
        services.AddSingleton<GitHubService>();

        serviceProvider = services.BuildServiceProvider();
    }
    private static ServiceProvider serviceProvider;

    private static string ToCSharpIdentifier(this string str)
    {
        var sb = new StringBuilder();
        foreach (var c in str)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('_');
            }
        }
        sb[0] = char.ToUpperInvariant(sb[0]);
        return sb.ToString();
    }

    public static async Task Main()
    {
        var json = JsonDocument.Parse(File.ReadAllText(@"E:\PROGRAMMING\Projects\C#\Tools\BlueskyDownloader\Resources\xxnrytuoaqir3eymj7kfcyci_3lllz33fw5k24\response.json"));
        var posts = json.RootElement.GetProperty("posts").EnumerateArray().First();
    }

    public static class LightsOutSolver
    {
        public static void Run()
        {
            // Initial and desired states in GF(2): true = light ON, false = light OFF
            bool[] pInitial = [true, true, true, true, true, false, true, false, true];
            bool[] bDesired = [true, true, true, true, true, true, true, true, true];

            // Transform/adjacency matrix in GF(2): rows/columns correspond to lights
            var adjacency = new bool[,]
            {
                { true, true,  false, true,  false, false, false, false, false },
                { true, true,  true,  false, true,  false, false, false, false },
                { false,true,  true,  false, false,true,  false, false, false },
                { true, false, false, true,  true,  false,true,  false, false },
                { false,true,  false, true,  true,  true,  false,true,  false },
                { false,false,true,  false, true,  true,  false,false,true  },
                { false,false,false,true,  false,false,true,  true,  false },
                { false,false,false,false,true,  false,true,  true,  true  },
                { false,false,false,false,false,true,  false,true,  true  }
            };

            // Calculate RHS = bDesired XOR pInitial
            var rhs = new bool[pInitial.Length];
            for (var i = 0; i < rhs.Length; i++)
                rhs[i] = bDesired[i] ^ pInitial[i];

            // Solve for toggles
            var solution = SolveLightsOut(adjacency, rhs);

            // Print out which switches should be pressed (1 = press, 0 = don't)
            for (var i = 0; i < solution.Length; i++)
                Console.WriteLine($"Switch {i + 1}: {(solution[i] ? 1 : 0)}");
        }

        private static bool[] SolveLightsOut(bool[,] matrix, bool[] rhs)
        {
            var n = rhs.Length;
            // Copy the matrix/rhs so we don't mutate the original
            var mat = new bool[n, n];
            var sol = new bool[n];
            for (var r = 0; r < n; r++)
            {
                for (var c = 0; c < n; c++)
                    mat[r, c] = matrix[r, c];
                sol[r] = rhs[r];
            }

            // Forward elimination in GF(2)
            for (var i = 0; i < n; i++)
            {
                // Find pivot row
                var pivot = i;
                while (pivot < n && !mat[pivot, i]) pivot++;
                if (pivot == n) continue; // No pivot in this column

                // Swap pivot row with current row if needed
                if (pivot != i)
                {
                    for (var col = 0; col < n; col++)
                    {
                        (mat[pivot, col], mat[i, col]) = (mat[i, col], mat[pivot, col]);
                    }
                    (sol[pivot], sol[i]) = (sol[i], sol[pivot]);
                }

                // Eliminate below pivot
                for (var row = i + 1; row < n; row++)
                {
                    if (mat[row, i])
                    {
                        for (var col = i; col < n; col++)
                            mat[row, col] ^= mat[i, col];
                        sol[row] ^= sol[i];
                    }
                }
            }

            // Back-substitution in GF(2)
            var result = new bool[n];
            for (var i = n - 1; i >= 0; i--)
            {
                var sum = sol[i];
                for (var col = i + 1; col < n; col++)
                    if (mat[i, col]) sum ^= result[col];

                // If mat[i, i] is false (no pivot), we can't solve for that variable definitively.
                // Typically, you'd choose a value (0) or track it's "free." For Lights Out,
                // we generally assume a pivot exists, but handle gracefully:
                result[i] = mat[i, i] && sum;
            }

            return result;
        }
    }

    private static async Task GitHubDotnetPoll()
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

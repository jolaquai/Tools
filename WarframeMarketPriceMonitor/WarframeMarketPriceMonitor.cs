using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;

using NAudio.Wave;

using Newtonsoft.Json;

namespace WarframeMarketPriceMonitor;

public static partial class WarframeMarketPriceMonitor
{
    private const string _baseUrl = "https://api.warframe.market/v1/items/{url_name}/orders";
    private const string _marketBaseUrl = "https://warframe.market/items/";
    private static readonly HttpClient _client = new HttpClient();

    private static readonly Assembly _assembly = typeof(WarframeMarketPriceMonitor).Assembly;

    private static Dictionary<string, int> _priceMonitors;
    private static TimeSpan _refreshInterval = TimeSpan.FromSeconds(10);

    public static async Task Main()
    {
        Console.WriteLine($"Warframe Market Price Monitor {_assembly.GetName().Version}");
        Console.WriteLine("Initializing...");

        AppDomain.CurrentDomain.ProcessExit += (s, e) => ConfigHandler.Save();

        ConfigHandler.Initialize();
        _priceMonitors = ConfigHandler.GetMonitors();

        Interop.PreventClose();

        Console.Clear();

        await MainMenuAsync();
    }

    private static class Operations
    {
        public const string Modify = "MODIFY";
        public const string List = "LIST";
        public const string Exit = "EXIT";
        public const string Begin = "BEGIN";
        public const string SetRefreshInterval = "SETREFRESH";
        public const string Save = "SAVE";

        public const string Back = "BACK";
        public const string Add = "ADD";
        public const string Remove = "REMOVE";
        public const string Edit = "EDIT";

        private static readonly string[] _operations = typeof(Operations).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => field.GetValue(null)!.ToString())
            .ToArray()!;
        public static bool IsValid(string operation) => Array.Exists(_operations, op => op == operation || op == operation.Split(' ')[0]);
    }
    private class AudioWrapper : IDisposable
    {
        private const string _audioName = "pikmin-gcn.mp3";
        private readonly AudioFileReader _reader = new AudioFileReader(_audioName);
        private readonly WaveOutEvent _device = new WaveOutEvent();

        public AudioWrapper()
        {
            _device.Init(_reader);
        }

        public void Dispose()
        {
            _reader.Dispose();
            _device.Dispose();
        }

        public void Play() => _device.Play();
    }

    private static string operation = "";
    private static async Task MainMenuAsync()
    {
        while (true)
        {
            Console.Clear();
            var split = operation.Split(' ');
            switch (split[0].ToUpperInvariant())
            {
                case Operations.Modify:
                {
                    operation = "";
                    ModifyMenu();
                    break;
                }
                case Operations.List:
                {
                    ListMonitors();
                    break;
                }
                case Operations.Begin:
                {
                    if (_priceMonitors.Count == 0)
                    {
                        goto case Operations.List;
                    }

                    await RunPriceMonitorsAsync();
                    break;
                }
                case Operations.SetRefreshInterval:
                {
                    if (split.Length != 2)
                    {
                        Console.WriteLine("Invalid command.");
                        operation = "";
                        break;
                    }

                    if (!int.TryParse(split[1], out var seconds) || seconds < 1)
                    {
                        Console.WriteLine("Invalid interval.");
                        operation = "";
                        break;
                    }

                    _refreshInterval = TimeSpan.FromSeconds(seconds);
                    break;
                }
                case Operations.Save:
                {
                    ConfigHandler.SetMonitors(_priceMonitors);
                    ConfigHandler.Save();
                    break;
                }
                case Operations.Exit:
                {
                    return;
                }
                case "":
                {
                    break;
                }
                default:
                {
                    Console.WriteLine("Invalid command.");
                    operation = "";
                    break;
                }
            }

            Console.WriteLine("------------------------");
            Console.WriteLine();

            Console.WriteLine($"""
                Commands
                  {Operations.Modify}
                    Enters item modification, listing all registered price monitors and allowing deletion, addition and modification.
                  {Operations.List}
                    Lists all registered price monitors.
                  {Operations.Begin}
                    Begins monitoring the prices of all registered items.
                    This will display all orders for each item at or below the specified price.
                    Press Ctrl+C to stop at any time.
                  {Operations.SetRefreshInterval} <seconds>
                    Changes the refresh interval for the price monitor (default: 10 seconds).
                    Warning: Setting this too low may result in rate limiting.
                  {Operations.Save}
                    Saves the current configuration to '{ConfigHandler.ConfigPath}'.

                Global commands (available at any time)
                  {Operations.Exit}
                    Exits the program.
                """);
            Console.WriteLine();
            Console.Write(">>");

            operation = Console.ReadLine();
        }
    }

    private static void ListMonitors()
    {
        if (_priceMonitors.Count == 0)
        {
            Console.WriteLine($"No price monitors registered. Add some using '{Operations.Modify}'.");
        }
        else
        {
            foreach (var (id, maxPrice) in _priceMonitors)
            {
                Console.WriteLine($"- Item: {id} | Max Price: {maxPrice}");
            }
        }
    }

    private static void ModifyMenu()
    {
        while (true)
        {
            Console.Clear();
            var split = operation.Split(' ');
            switch (split[0].ToUpperInvariant())
            {
                case Operations.Add:
                {
                    if (split.Length != 3)
                    {
                        Console.WriteLine("Invalid command.");
                        operation = "";
                        break;
                    }

                    var id = split[1];
                    if (_priceMonitors.ContainsKey(id))
                    {
                        Console.WriteLine($"A monitor for this item already exists. Use '{Operations.Edit}' instead.");
                        operation = "";
                        break;
                    }

                    if (!int.TryParse(split[2], out var maxPrice) || maxPrice < 0)
                    {
                        Console.WriteLine("Invalid max price.");
                        operation = "";
                        break;
                    }

                    _priceMonitors.Add(id, maxPrice);
                    ConfigHandler.SetMonitors(_priceMonitors);
                    break;
                }
                case Operations.Remove:
                {
                    if (split.Length != 2)
                    {
                        Console.WriteLine("Invalid command.");
                        operation = "";
                        break;
                    }

                    var id = split[1];
                    if (!_priceMonitors.ContainsKey(id))
                    {
                        Console.WriteLine("No monitor for this item exists.");
                        operation = "";
                        break;
                    }

                    _priceMonitors.Remove(id);
                    ConfigHandler.SetMonitors(_priceMonitors);
                    break;
                }
                case Operations.Edit:
                {
                    if (split.Length != 3)
                    {
                        Console.WriteLine("Invalid command.");
                        operation = "";
                        break;
                    }

                    var id = split[1];
                    if (!_priceMonitors.ContainsKey(id))
                    {
                        Console.WriteLine("No monitor for this item exists.");
                        operation = "";
                        break;
                    }

                    if (!int.TryParse(split[2], out var maxPrice) || maxPrice < 0)
                    {
                        Console.WriteLine("Invalid max price.");
                        operation = "";
                        break;
                    }

                    _priceMonitors[id] = maxPrice;
                    ConfigHandler.SetMonitors(_priceMonitors);
                    break;
                }
                case Operations.Back:
                {
                    return;
                }
                case Operations.Exit:
                {
                    Environment.Exit(0);
                    break;
                }
                case "":
                {
                    break;
                }
                default:
                {
                    Console.WriteLine("Invalid command.");
                    operation = "";
                    break;
                }
            }

            Console.WriteLine("------------------------");
            Console.WriteLine();

            Console.WriteLine("Monitor Modification");
            ListMonitors();
            Console.WriteLine();

            Console.WriteLine($"""
                Commands
                  {Operations.Add} <item_id> <max_price>
                    Adds a new price monitor for the specified item.
                  {Operations.Remove} <item_id>
                    Removes an existing price monitor for the specified item.
                    This does nothing if a monitor for the specified item does not exist.
                  {Operations.Edit} <item_id> <new_max_price>
                    Changes the max price of an existing price monitor.
                    This does nothing if a monitor for the specified item does not exist.

                  {Operations.Back}
                    Returns to the main menu.
                """);
            Console.WriteLine();
            Console.Write(">>");

            operation = Console.ReadLine();
        }
    }

    private static async Task RunPriceMonitorsAsync()
    {
        Console.Clear();
        Console.WriteLine("Running price monitors...");
        Console.WriteLine("Press Ctrl+C to stop at any time. This'll bring you back to the main menu.");
        Console.WriteLine();

        var cts = new CancellationTokenSource();
        void PreventCancel(object sender, ConsoleCancelEventArgs e)
        {
            cts.Cancel();
            e.Cancel = true;
        }
        using var audio = new AudioWrapper();

        Console.CancelKeyPress += PreventCancel;

        try
        {
            while (!cts.IsCancellationRequested)
            {
                foreach (var (id, maxPrice) in _priceMonitors)
                {
                    var requestUrl = _baseUrl.Replace("{url_name}", id);
                    var marketUrl = _marketBaseUrl + id;

                    using var response = await _client.GetAsync(requestUrl, cts.Token);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync(cts.Token);
                    string ordersJarray = (((dynamic)JsonConvert.DeserializeObject(json)).payload.orders).ToString();
                    var orders = JsonConvert.DeserializeObject<order[]>(ordersJarray);

                    orders = Array.FindAll(orders, order => order.user.status == "ingame");
                    orders = Array.FindAll(orders, order => order.visible);
                    orders = Array.FindAll(orders, order => order.order_type == "sell");
                    orders = Array.FindAll(orders, order => order.platform == "pc");
                    orders = Array.FindAll(orders, order => order.platinum <= maxPrice);

                    if (orders.Length > 0)
                    {
                        audio.Play();

                        var usernamePadding = orders.Max(order => order.user.ingame_name.Length);
                        var quantityPadding = orders.Max(order => order.quantity.ToString().Length) + 1;
                        var platinumPadding = orders.Max(order => order.platinum.ToString().Length);

                        Array.Sort(orders, (x, y) => x.platinum.CompareTo(y.platinum));

                        Console.WriteLine($"Found {orders.Length} orders for {id}:");
                        Console.WriteLine($"Open here: '{marketUrl}'");
                        foreach (var order in orders)
                        {
                            Console.Write("    - ");
                            Console.Write(order.user.ingame_name.PadRight(usernamePadding));
                            Console.Write(" | ");
                            Console.Write((order.quantity.ToString() + 'x').PadRight(quantityPadding));
                            Console.Write(" | ");
                            Console.Write(order.platinum.ToString().PadRight(platinumPadding));
                            Console.WriteLine(" Platinum");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"No offers found for {id} <= {maxPrice} Platinum.");
                    }
                }

                await Task.Delay(_refreshInterval, cts.Token);
                Console.Clear();
            }
        }
        catch (OperationCanceledException)
        {
            Console.Clear();
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"The app is being rate-limited. Please increase the refresh interval using '{Operations.SetRefreshInterval}'.");
            Console.WriteLine("Cancelling price monitoring.");
            Console.ResetColor();
            cts.Cancel();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.WriteLine("Cancelling price monitoring.");
            Console.ResetColor();
            cts.Cancel();
        }

        Console.CancelKeyPress -= PreventCancel;
    }

    private static partial class Interop
    {
        private const int MF_BYCOMMAND = 0;
        private const int SC_CLOSE = 0xF060;

        [LibraryImport("user32.dll")]
        private static partial int DeleteMenu(nint hMenu, int nPosition, int wFlags);
        [LibraryImport("user32.dll")]
        private static partial nint GetSystemMenu(nint hWnd, [MarshalAs(UnmanagedType.Bool)] bool bRevert);
        [LibraryImport("kernel32.dll")]
        private static partial nint GetConsoleWindow();

        public static void PreventClose()
        {
            DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), SC_CLOSE, MF_BYCOMMAND);
        }
    }
}

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;

using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;

namespace SteamApiTest;

public static class Assert
{
    public class AssertionFailureException(string expression) : Exception
    {
        public override string Message => $"Assertion failed by violating expression: '{expression}'";
    }

    public static void That([DoesNotReturnIf(false)] bool condition, [CallerArgumentExpression(nameof(condition))] string expr = "")
    {
        if (!condition)
        {
            throw new AssertionFailureException(expr);
        }
    }
}

public static class Interop
{
    private static readonly Encoding _enc = Encoding.ASCII;

    public static byte[] FromAscii(string byteRepr)
    {
        var buf = new byte[byteRepr.Length / 2];

        var span = byteRepr.AsSpan();
        var start = 0;
        var i = 0;
        while (start < byteRepr.Length)
        {
            var slice = span[start..(start + 2)];
            buf[i++] = byte.Parse(slice, NumberStyles.HexNumber);
            start += 2;
        }
        return buf;
    }

    public static string StringFromBytes(this byte[] bytes, ref nint ptr)
    {
        // Read from bytes[readFrom] until encountering a \0
        var start = ptr;
        while (bytes[ptr++] != 0) { }
        unsafe
        {
            fixed (byte* p = &bytes[start])
            {
                return _enc.GetString(p, (int)(ptr - 1 - start));
            }
        }
    }

    public static T Read<T>(this byte[] bytes, ref nint ptr)
    {
        var typeOfT = typeof(T);
        if (!typeOfT.IsValueType && typeOfT != typeof(string))
        {
            throw new ArgumentException("T must be a value type or string.");
        }

        if (typeOfT == typeof(string))
        {
            return (T)(object)bytes.StringFromBytes(ref ptr);
        }
        else if (typeOfT == typeof(byte))
        {
            return (T)(object)bytes[ptr++];
        }
        else if (typeOfT == typeof(bool))
        {
            return (T)(object)(bytes[ptr++] != 0);
        }

        T value;
        unsafe
        {
            var incr = sizeof(T);
            fixed (void* p = &bytes[ptr])
            {
                var tPtr = (T*)p;
                value = *tPtr;
                ptr += incr;
            }
        }

        return value;
    }
}

internal static class GmodReconnector
{
    private const string _steamApiKey = "AF18C39B1BD7ED5FF77756BAA64AD6CA";
    private const string _ip = "212.132.106.58";
    private const int _port = 25565;
    private static readonly string _sgIp = $"{_ip}:{_port}";

    private static readonly HttpClient _client = new HttpClient();

    private static readonly SteamWebInterfaceFactory interfaceFactory = new SteamWebInterfaceFactory(_steamApiKey);
    private static readonly SteamUser steamUser = interfaceFactory.CreateSteamWebInterface<SteamUser>(_client);

    private const ulong _kroneSteamId = 76561198803087973;
    private static readonly TimeSpan _defaultDelay = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan _exceptDelay = TimeSpan.FromMinutes(15);

    public record struct Player
    {
        public byte playerIndex;
        public string playerName;
        public int score;
        public TimeSpan onlineSeconds;
    }

    private static readonly UdpClient _udpClient = new UdpClient();
    private static bool connected;
    public static async Task<List<Player?>?> QueryServer()
    {
        if (!connected)
        {
            _udpClient.Client.Connect(IPAddress.Parse(_ip), _port);
            connected = true;
        }

        try
        {
            var challengeData = Interop.FromAscii("ffffffff55ffffffff");
            var sent = await _udpClient.Client.SendAsync(challengeData).ConfigureAwait(false);
            Assert.That(sent == challengeData.Length);

            var challengeResponseBuf = new byte[4];
            var result = await _udpClient.ReceiveAsync().ConfigureAwait(false);
            result.Buffer.AsSpan(5).CopyTo(challengeResponseBuf);

            byte[] a2s_playerData = [.. Interop.FromAscii("ffffffff55"), .. challengeResponseBuf];
            sent = await _udpClient.Client.SendAsync(a2s_playerData).ConfigureAwait(false);
            Assert.That(sent == a2s_playerData.Length);

            result = await _udpClient.ReceiveAsync().ConfigureAwait(false);
            var returnData = new byte[result.Buffer.Length];
            result.Buffer.AsSpan().CopyTo(returnData);

            nint ptr = 5;
            var chunkCount = returnData.Read<byte>(ref ptr);

            List<Player?> players = [];
            while (ptr < returnData.Length)
            {
                players.Add(default(Player) with
                {
                    playerIndex = returnData[ptr++],
                    playerName = returnData.Read<string>(ref ptr),
                    score = returnData.Read<int>(ref ptr),
                    onlineSeconds = TimeSpan.FromSeconds((int)returnData.Read<float>(ref ptr)),
                });
            }

            return players;
        }
        catch
        {
            return null;
        }
    }

    public static async Task Main()
    {
        Console.WriteLine($"=== {nameof(GmodReconnector)} ===");
        Console.WriteLine();

        while (true)
        {
            try
            {
                var userStats = (await steamUser.GetPlayerSummaryAsync(_kroneSteamId)).Data;
                if (userStats.PlayingGameServerIP is not string ip || !ip.Equals(_sgIp, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Detected server disconnect. Sending Steam protocol request to join...");
                    SendGmodConnectSteamCall();
                }
                else
                {
                    // Steam thinks we're still connected, but this does not accurately reflect Gmod's state
                    // Gmod could be stuck in the confirmation dialog that pops up after a forced server restart

                    var players = await QueryServer();
                    if (players is null)
                    {
                        Console.WriteLine("Something went wrong while fetching server data (is it offline?)");
                    }
                    else if (players.Find(p => p!.Value.playerName.Contains("KorOwOne", StringComparison.OrdinalIgnoreCase)) is not Player)
                    {
                        Console.WriteLine("Steam says we're still connected, but the server doesn't agree. Asking Gmod to reconnect...");
                        SendGmodConnectSteamCall();
                    }
                    else
                    {
                        Console.WriteLine("Still in-game...");
                    }
                }

                await Task.Delay(_defaultDelay);
            }
            catch (Exception ex)
            {
                Console.WriteException(ex);
                await Task.Delay(_exceptDelay);
            }
        }
    }

    private static void SendGmodConnectSteamCall()
    {
        var proc = Process.Start(new ProcessStartInfo()
        {
            UseShellExecute = true,
            FileName = $"steam://connect/{_sgIp}"
        });
        Console.WriteLine("Gmod should now be reconnecting or connected back to the server.");
        proc?.WaitForExit();
    }
}
internal static class Console
{
    public static void WriteLine() => System.Console.WriteLine();
    public static void WriteLine<T>(T obj) => System.Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {obj}");
    public static void Write<T>(T obj) => System.Console.Write($"[{DateTime.Now:HH:mm:ss}] {obj}");
    public static void WriteException(Exception ex) => WriteException(null, ex);
    public static void WriteException(string message, Exception ex)
    {
        var now = DateTime.Now;
        var exType = ex.GetType();

        System.Console.ForegroundColor = ConsoleColor.DarkRed;

        if (message is not null)
        {
            System.Console.WriteLine($"[{now:HH:mm:ss}] {message}");
        }
        else
        {
            System.Console.WriteLine($"[{now:HH:mm:ss}]");
        }
        System.Console.WriteLine($"    [{exType.Namespace}.{exType.Name}] {ex.Message}");
        System.Console.WriteLine($"      from: {ex.TargetSite?.Name ?? "(no target site found)"}");
        System.Console.WriteLine($"    {ex.StackTrace}");

        ex = ex.InnerException;
        var nestCount = 1;
        while (ex is not null)
        {
            var indent = new string(' ', 4 + (nestCount * 2));
            exType = ex.GetType();

            System.Console.WriteLine(indent + $"Inner exception: [{exType.Namespace}.{exType.Name}] {ex.Message}");
            System.Console.WriteLine(indent + $"  from: {ex.TargetSite?.Name ?? "(no target site found)"}");
            System.Console.WriteLine(indent + $"{ex.StackTrace}");

            ex = ex.InnerException;
            nestCount++;
        }

        System.Console.ResetColor();
    }
}

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace DbdOverlay;

public static partial class Interop
{
    public struct LPRECT : IEquatable<LPRECT>
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is LPRECT other && Equals(other);
        public bool Equals(LPRECT other) => Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;
    }
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(nint hWnd, out LPRECT lpRect);
    public static LPRECT? GetWindowBounds(nint? hWnd)
    {
        if (hWnd is not null)
        {
            GetWindowRect(hWnd.Value, out var rect);
            return rect;
        }
        return default;
    }

    public class WindowBoundsMonitor
    {
        private LPRECT? bounds;
        private volatile bool running;
        private Timer timer;

        public event EventHandler<LPRECT?> BoundsChanged;

        public WindowBoundsMonitor(string procName)
        {
            timer = new Timer(_ =>
            {
                if (running) return;

                running = true;
                try
                {
                    using var dbdProc = Process.GetProcessesByName(procName).FirstOrDefault();
                    var newBounds = GetWindowBounds(dbdProc?.MainWindowHandle);

                    // Also call the event if the bounds are null
                    if (!bounds?.Equals(newBounds) is not false)
                    {
                        bounds = newBounds;
                        BoundsChanged?.Invoke(this, bounds);
                    }
                }
                finally
                {
                    running = false;
                }
            }, null, 500, 500);
        }
    }
}

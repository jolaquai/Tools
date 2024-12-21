namespace Monitors.Shared.Util;
public static class CancellationTokenExtensions
{
    /// <summary>
    /// Creates a <see cref="Task"/> that completes successfully when the specified <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    public static Task WhenCancelled(this CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource();
        cancellationToken.Register(tcs.SetResult);
        return tcs.Task;
    }
}

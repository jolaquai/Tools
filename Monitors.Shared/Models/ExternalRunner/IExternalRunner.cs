﻿namespace Monitors.Shared.Models.ExternalRunner;

public interface IExternalRunner
{
    /// <summary>
    /// Determines whether the external object can be handled by this runner.
    /// </summary>
    /// <returns><see langword="true"/> if the runner can handle the external object; otherwise, <see langword="false"/>.</returns>
    bool CanHandle(string path);
    /// <summary>
    /// Runs the external object.
    /// </summary>
    /// <returns>A <see cref="Task"/> that represents the lifetime of the external object. Cancellation propagated through <paramref name="stoppingToken"/> should stop the external object.</returns>
    Task RunAsync(string path, string[] args, CancellationToken stoppingToken);
}

using System;

using Microsoft.Extensions.DependencyInjection;

namespace _Random;

public static partial class Program
{
    static Program()
    {
        var services = new ServiceCollection();

        serviceProvider = services.BuildServiceProvider();
    }
    private static ServiceProvider serviceProvider;

    public static async Task Main(params string[] args)
    {
        _ = args;
    }
}

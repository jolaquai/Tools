using Microsoft.Extensions.DependencyInjection;

namespace _Random;

public static partial class Program
{
    private static ServiceProvider Setup()
    {
        var services = new ServiceCollection();

        return services.BuildServiceProvider();
    }

    private static ServiceProvider serviceProvider = Setup();

    public static async Task Main(params string[] args)
    {
        _ = args; 
    }
}

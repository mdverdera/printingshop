using Microsoft.Extensions.DependencyInjection;
using printingshop.infrastructure;

namespace printingshop.lambda;

/// <summary>
/// Lambda startup configuration using dependency injection
/// </summary>
public static class LambdaStartup
{
    private static IServiceProvider? _serviceProvider;

    /// <summary>
    /// Gets or creates the service provider for Lambda functions
    /// </summary>
    public static IServiceProvider GetServiceProvider()
    {
        _serviceProvider ??= DependencyInjection.BuildServiceProvider();
        return _serviceProvider;
    }
}

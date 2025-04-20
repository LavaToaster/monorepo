using Microsoft.Extensions.DependencyInjection;

namespace DBot.Integrations.Assetto.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAssettoServerClient(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddSingleton<AssettoServerClientFactory>();

        return services;
    }
}
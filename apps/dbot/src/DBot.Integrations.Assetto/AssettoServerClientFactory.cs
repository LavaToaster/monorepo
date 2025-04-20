using Refit;

namespace DBot.Integrations.Assetto;

public class AssettoServerClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AssettoServerClientFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IAssettoServerApi CreateClient(string baseUrl)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl);

        return RestService.For<IAssettoServerApi>(client);
    }
}
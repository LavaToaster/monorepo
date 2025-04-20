using DBot.Integrations.Assetto.Models;
using Refit;

namespace DBot.Integrations.Assetto;

public interface IAssettoServerApi
{
    [Get("/api/details")]
    public Task<DetailResponse> GetServerDetailsAsync();
}
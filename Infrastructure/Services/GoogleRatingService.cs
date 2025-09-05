using System.Text.Json;
using Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public class GoogleRatingService : IGoogleRatingService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GoogleRatingService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GoogleApiKey"];
    }

    public async Task<double?> GetRatingAsync(string businessName, string address, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
            return null;

        var query = $"{businessName} {address}";
        var url = $"https://maps.googleapis.com/maps/api/place/textsearch/json?query={Uri.EscapeDataString(query)}&key={_apiKey}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        try
        {
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var results = json.RootElement.GetProperty("results");
            if (results.GetArrayLength() > 0)
            {
                var first = results[0];
                if (first.TryGetProperty("rating", out var ratingElem) && ratingElem.TryGetDouble(out var rating))
                {
                    return rating;
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
    }
}

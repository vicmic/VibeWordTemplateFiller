using System.Net.Http;
using System.Text.Json;
using DistanceCalculator.Models.GoogleApi;

namespace DistanceCalculator.Services;

public class GoogleMapsService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://maps.googleapis.com/maps/api/distancematrix/json";

    public GoogleMapsService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public async Task<(double? DistanceKm, string? Error)> GetDistanceAsync(
        string origin, string destination, string apiKey, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}?origins={Uri.EscapeDataString(origin)}" +
                  $"&destinations={Uri.EscapeDataString(destination)}" +
                  $"&key={apiKey}&language=it&units=metric";

        var json = await _httpClient.GetStringAsync(url, cancellationToken);
        var response = JsonSerializer.Deserialize<DistanceMatrixResponse>(json);

        if (response is null)
            return (null, "Risposta non valida dall'API");

        if (response.Status != "OK")
            return (null, MapApiStatus(response.Status, response.ErrorMessage));

        var element = response.Rows?.FirstOrDefault()?.Elements?.FirstOrDefault();
        if (element is null)
            return (null, "Nessun risultato restituito");

        if (element.Status != "OK")
            return (null, MapElementStatus(element.Status));

        return (element.Distance!.Value / 1000.0, null);
    }

    public async Task<(bool Valid, string Message)> ValidateApiKeyAsync(string apiKey)
    {
        try
        {
            var (dist, error) = await GetDistanceAsync("Roma, Italia", "Milano, Italia", apiKey);
            if (error is null && dist.HasValue)
                return (true, $"Chiave API valida (test: Roma→Milano = {dist:F0} km)");
            return (false, error ?? "Risposta non valida");
        }
        catch (Exception ex)
        {
            return (false, $"Errore di connessione: {ex.Message}");
        }
    }

    private static string MapApiStatus(string status, string? message) => status switch
    {
        "REQUEST_DENIED" => message ?? "Chiave API non valida o non autorizzata",
        "OVER_DAILY_LIMIT" => "Limite giornaliero superato",
        "OVER_QUERY_LIMIT" => "Limite query superato",
        "INVALID_REQUEST" => "Richiesta non valida",
        "UNKNOWN_ERROR" => "Errore sconosciuto del server Google",
        _ => $"Errore API: {status}"
    };

    private static string MapElementStatus(string status) => status switch
    {
        "NOT_FOUND" => "Indirizzo non trovato",
        "ZERO_RESULTS" => "Nessun percorso trovato tra questi indirizzi",
        "MAX_WAYPOINTS_EXCEEDED" => "Troppi waypoint",
        _ => $"Errore elemento: {status}"
    };
}

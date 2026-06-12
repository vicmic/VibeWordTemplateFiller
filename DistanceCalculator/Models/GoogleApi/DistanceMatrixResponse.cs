using System.Text.Json.Serialization;

namespace DistanceCalculator.Models.GoogleApi;

public class DistanceMatrixResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("origin_addresses")]
    public string[]? OriginAddresses { get; set; }

    [JsonPropertyName("destination_addresses")]
    public string[]? DestinationAddresses { get; set; }

    [JsonPropertyName("rows")]
    public Row[]? Rows { get; set; }

    public class Row
    {
        [JsonPropertyName("elements")]
        public Element[]? Elements { get; set; }
    }

    public class Element
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("distance")]
        public ValueText? Distance { get; set; }

        [JsonPropertyName("duration")]
        public ValueText? Duration { get; set; }
    }

    public class ValueText
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("value")]
        public int Value { get; set; }
    }
}

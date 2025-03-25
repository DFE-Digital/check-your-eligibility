using System.Text.Json.Serialization;

namespace CheckYourEligibility.API.Boundary.Requests.DWP;

public class CitizenMatchRequest
{
    [JsonPropertyName("jsonapi")] public CitizenMatchRequest_Jsonapi Jsonapi { get; set; }

    [JsonPropertyName("data")] public CitizenMatchRequest_Data Data { get; set; }

    public class CitizenMatchRequest_Data
    {
        [JsonPropertyName("type")] public string Type { get; set; }

        [JsonPropertyName("attributes")] public CitizenMatchRequest_Attributes Attributes { get; set; }
    }

    public class CitizenMatchRequest_Jsonapi
    {
        [JsonPropertyName("version")] public string Version { get; set; }
    }

    public class CitizenMatchRequest_Attributes
    {
        [JsonPropertyName("dateOfBirth")] public string DateOfBirth { get; set; }

        [JsonPropertyName("ninoFragment")] public string NinoFragment { get; set; }

        [JsonPropertyName("lastName")] public string LastName { get; set; }
    }
}
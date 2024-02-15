using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CheckYourEligibility.Domain.Requests
{
    public class CheckEligibilityRequestData
    {
        [JsonProperty(PropertyName = "nationalInsuranceNumber")]
        public string NiNumber { get; set; } //nationalInsuranceNumber

        [JsonProperty(PropertyName = "LastName")]
        public string LastName { get; set; }

        [JsonProperty(PropertyName = "dateOfBirth")]
        public string DateOfBirth { get; set; }

        [JsonProperty(PropertyName = "nationalAsylumSeekerServiceNumber")]
        public string NASSNumber { get; set; } //national asylum seeker service number
    }
}
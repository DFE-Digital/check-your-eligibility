using System.Text.Json.Serialization;
using CheckYourEligibility.API.Domain.Enums.WorkingFamilies;

namespace CheckYourEligibility.API.Boundary.Responses
{

    public class FosterChildResponse
    {
        // Eligibility Code Details

        public string EligibilityCode { get; set; } = string.Empty;

        public string ReconfirmationStatus { get; set; }

        public Term ValidFromTerm { get; set; }

        [JsonIgnore]
        public DateTime ValidityStartDate { get; set; }
        [JsonIgnore]
        public DateTime ValidityEndDate { get; set; }

        public DateTime ReconfirmBetweenStart { get; set; }

        public DateTime ReconfirmBetweenEnd { get; set; }

        public DateTime GracePeriodEndDate { get; set; }


        // Child

        public Guid FosterChildId { get; set; }

        public string ChildFullName { get; set; }

        public DateTime ChildDateOfBirth { get; set; }

        public string PostCode { get; set; }


        // Foster Family

        public Guid? FosterCarerId { get; set; }

        public string? CarerName { get; set; }

        public string? PartnerName { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace CheckYourEligibility.Data.Models
{
    [ExcludeFromCodeCoverage(Justification = "Data Model.")]
    public class LocalAuthority
    {
        [Key]
        public int LocalAuthorityId { get; set; }
        public string LaName { get; set; }
    }
}

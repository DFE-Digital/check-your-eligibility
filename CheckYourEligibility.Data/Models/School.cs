using System.ComponentModel.DataAnnotations;

namespace CheckYourEligibility.Data.Models
{
    public class School
    {
        [Key]
        public int Urn { get; set; }
        public string EstablishmentName { get; set; }
        public string Postcode { get; set; }
        public string Street { get; set; }
        public string Locality { get; set; }
        public string Town { get; set; }
        public string County { get; set; }
        public string Status { get; set; }
        public int LocalAuthorityLaCode { get; set; }
        public virtual LocalAuthority LocalAuthority { get; set; }
    }
}

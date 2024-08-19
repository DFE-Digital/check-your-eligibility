

// Ignore Spelling: Fsm

using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace CheckYourEligibility.Data.Models
{
    [ExcludeFromCodeCoverage(Justification = "Data Model.")]
    public class User
    {
        public string UserID { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Email { get; set; }

        [Column(TypeName = "varchar(1000)")]
        public string Reference { get; set; }
    }
}

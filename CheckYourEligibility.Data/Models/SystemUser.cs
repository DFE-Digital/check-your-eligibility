

// Ignore Spelling: Fsm

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace CheckYourEligibility.Data.Models
{
    [ExcludeFromCodeCoverage(Justification = "Data Model.")]
    public class SystemUser
    {
        [Key]
        [Required]
        [Column(TypeName = "varchar(200)")]
        public string UserName { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Password { get; set; }
        public IList<string> Roles { get; set; }
    }
}

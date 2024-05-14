

// Ignore Spelling: Fsm

using CheckYourEligibility.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace CheckYourEligibility.Data.Models
{
    public class User
    {
        public string UserID { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Email { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Reference { get; set; }
    }
}

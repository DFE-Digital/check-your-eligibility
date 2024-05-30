

// Ignore Spelling: Fsm

using CheckYourEligibility.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace CheckYourEligibility.Data.Models
{
    [ExcludeFromCodeCoverage(Justification = "Data Model.")]
    public class EligibilityCheck
    {
        public string EligibilityCheckID { get; set; }

        [Column(TypeName = "varchar(100)")]
        public CheckEligibilityType Type { get; set; }

        [Column(TypeName = "varchar(100)")]
        public CheckEligibilityStatus Status { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? NINumber { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string? NASSNumber { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string LastName { get; set; }

        public DateTime DateOfBirth { get; set; }

        public  string? EligibilityCheckHashID { get; set; }

        public virtual EligibilityCheckHash? EligibilityCheckHash { get; set; }


    }
}

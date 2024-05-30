

// Ignore Spelling: Fsm

using CheckYourEligibility.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace CheckYourEligibility.Data.Models
{
    [ExcludeFromCodeCoverage(Justification = "Data Model.")]
    public class EligibilityCheckHash
    {
        public string EligibilityCheckHashID { get; set; }
        [Column(TypeName = "varchar(5000)")]

        public string Hash { get; set; }
        
        [Column(TypeName = "varchar(100)")]
        public CheckEligibilityType Type { get; set; }

        public DateTime TimeStamp { get; set; }
        [Column(TypeName = "varchar(100)")]

        public CheckEligibilityStatus Outcome { get; set; }

        [Column(TypeName = "varchar(100)")]
        public ProcessEligibilityCheckSource Source { get; set; }
    }
}

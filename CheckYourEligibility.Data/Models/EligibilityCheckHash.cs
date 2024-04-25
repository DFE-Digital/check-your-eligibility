

// Ignore Spelling: Fsm

using CheckYourEligibility.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace CheckYourEligibility.Data.Models
{
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

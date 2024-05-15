

// Ignore Spelling: Fsm

using CheckYourEligibility.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace CheckYourEligibility.Data.Models
{
    public class Audit
    {
        public string AuditID { get; set; }
        public AuditType Type { get; set; }
        [Column(TypeName = "varchar(200)")]
        public string typeId { get; set; }
        [Column(TypeName = "varchar(200)")]
        public string url { get; set; }
        [Column(TypeName = "varchar(200)")]
        public string method { get; set; }
        [Column(TypeName = "varchar(500)")]
        public string source { get; set; }
        [Column(TypeName = "varchar(5000)")]
        public string authentication { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}


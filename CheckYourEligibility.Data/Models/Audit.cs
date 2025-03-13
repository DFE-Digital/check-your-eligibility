﻿

// Ignore Spelling: Fsm

using CheckYourEligibility.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace CheckYourEligibility.Data.Models
{
    [ExcludeFromCodeCoverage(Justification = "Data Model.")]
    public class Audit
    {
        public string AuditID { get; set; }
        [Column(TypeName = "varchar(100)")]
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
        
        [Column(TypeName = "varchar(100)")]
        public string? scope { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}


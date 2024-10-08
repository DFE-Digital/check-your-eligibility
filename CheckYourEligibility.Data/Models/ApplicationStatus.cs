﻿

// Ignore Spelling: Fsm

using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace CheckYourEligibility.Data.Models
{
    [ExcludeFromCodeCoverage(Justification = "Data Model.")]
    public class ApplicationStatus
    {
        public string ApplicationStatusID { get; set; }
        public string ApplicationID { get; set; }
        public virtual Application Application { get; set; }

        [Column(TypeName = "varchar(100)")]
        public Domain.Enums.ApplicationStatus Type { get; set; }

        public DateTime TimeStamp { get; set; }

    }
}

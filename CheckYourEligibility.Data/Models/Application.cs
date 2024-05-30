

// Ignore Spelling: Fsm

using CheckYourEligibility.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace CheckYourEligibility.Data.Models
{
    [ExcludeFromCodeCoverage(Justification = "Data Model.")]
    public class Application
    {
        public string ApplicationID { get; set; }

        [Column(TypeName = "varchar(100)")]
        public CheckEligibilityType Type { get; set; }

        [Column(TypeName = "varchar(8)")]
        public string Reference { get; set; }

        public int LocalAuthorityId { get; set; }

        public virtual School School { get; set; }
        public int SchoolId { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string ParentFirstName { get; set; }
        
        [Column(TypeName = "varchar(100)")]
        public string ParentLastName { get; set; }
        
        [Column(TypeName = "varchar(50)")]
        public string? ParentNationalInsuranceNumber { get; set; }
        
        [Column(TypeName = "varchar(50)")]
        public string? ParentNationalAsylumSeekerServiceNumber { get; set; }

        public DateTime ParentDateOfBirth { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string ChildFirstName { get; set; }
        
        [Column(TypeName = "varchar(50)")]
        public string ChildLastName { get; set; }

        public DateTime ChildDateOfBirth { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        public virtual IEnumerable<ApplicationStatus>  Statuses { get; set; }
        [Column(TypeName = "varchar(100)")]
        public Domain.Enums.ApplicationStatus? Status { get; set; }

        public virtual User User { get; set; }
        public string? UserId { get; set; }
    }
}

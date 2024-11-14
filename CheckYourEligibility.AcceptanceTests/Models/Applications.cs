﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace CheckYourEligibility.AcceptanceTests.Models;

public partial class Applications
{
    public string ApplicationId { get; set; }

    public string Type { get; set; }

    public string Reference { get; set; }

    public int LocalAuthorityId { get; set; }

    public int EstablishmentId { get; set; }

    public string ParentFirstName { get; set; }

    public string ParentLastName { get; set; }

    public string ParentNationalInsuranceNumber { get; set; }

    public string ParentNationalAsylumSeekerServiceNumber { get; set; }

    public DateTime ParentDateOfBirth { get; set; }

    public string ChildFirstName { get; set; }

    public string ChildLastName { get; set; }

    public DateTime ChildDateOfBirth { get; set; }

    public DateTime Created { get; set; }

    public DateTime Updated { get; set; }

    public string Status { get; set; }

    public string UserId { get; set; }

    public string EligibilityCheckHashId { get; set; }

    public string ParentEmail { get; set; }

    public virtual ICollection<ApplicationStatuses> ApplicationStatuses { get; set; } = new List<ApplicationStatuses>();

    public virtual EligibilityCheckHashes EligibilityCheckHash { get; set; }

    public virtual Establishments Establishment { get; set; }

    public virtual Users User { get; set; }
}
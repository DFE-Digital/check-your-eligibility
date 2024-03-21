using CheckYourEligibility.Data.Models;
using Microsoft.EntityFrameworkCore;

public interface IEligibilityCheckContext
{
    DbSet<EligibilityCheck> FsmCheckEligibilities { get; set; }
    DbSet<FreeSchoolMealsHMRC> FreeSchoolMealsHMRC { get; set; }
    DbSet<FreeSchoolMealsHO> FreeSchoolMealsHO { get; set; }
    DbSet<School> Schools { get; set; }
    DbSet<LocalAuthority> LocalAuthorities { get; set; }
    DbSet<Application> Applications { get; set; }
    DbSet<ApplicationStatus> ApplicationStatuses { get; set; }

    Task<int> SaveChangesAsync();
}
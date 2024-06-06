using CheckYourEligibility.Data.Models;
using Microsoft.EntityFrameworkCore;

public interface IEligibilityCheckContext
{
    DbSet<EligibilityCheck> CheckEligibilities { get; set; }
    DbSet<FreeSchoolMealsHMRC> FreeSchoolMealsHMRC { get; set; }
    DbSet<FreeSchoolMealsHO> FreeSchoolMealsHO { get; set; }
    DbSet<School> Schools { get; set; }
    DbSet<LocalAuthority> LocalAuthorities { get; set; }
    DbSet<Application> Applications { get; set; }
    DbSet<ApplicationStatus> ApplicationStatuses { get; set; }
    DbSet<EligibilityCheckHash> EligibilityCheckHashes { get; set; }
    DbSet<User> Users { get; set; }
    DbSet<Audit> Audits { get; set; }

    void BulkInsert_FreeSchoolMealsHO(IEnumerable<FreeSchoolMealsHO> data);
    Task<int> SaveChangesAsync();
    int SaveChanges();
    void BulkInsert_FreeSchoolMealsHMRC(IEnumerable<FreeSchoolMealsHMRC> data);
}
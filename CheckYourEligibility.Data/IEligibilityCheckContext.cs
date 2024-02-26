using CheckYourEligibility.Data.Models;
using Microsoft.EntityFrameworkCore;

public interface IEligibilityCheckContext
{
    DbSet<FsmCheckEligibility> FsmCheckEligibilities { get; set; }
    DbSet<FreeSchoolMealsHMRC> FreeSchoolMealsHMRC { get; set; }
    DbSet<FreeSchoolMealsHO> FreeSchoolMealsHO { get; set; }

    void SaveChangesAsync();
}
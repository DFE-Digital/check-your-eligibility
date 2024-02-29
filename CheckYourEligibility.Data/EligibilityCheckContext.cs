// Ignore Spelling: Fsm

using CheckYourEligibility.Data.Enums;
using CheckYourEligibility.Data.Models;
using Microsoft.EntityFrameworkCore;

public class EligibilityCheckContext : DbContext, IEligibilityCheckContext
{
    public EligibilityCheckContext(DbContextOptions<EligibilityCheckContext> options) : base(options)
    {
    }

    public virtual  DbSet<EligibilityCheck> FsmCheckEligibilities { get; set; }
    public virtual DbSet<FreeSchoolMealsHMRC> FreeSchoolMealsHMRC { get; set; }
    public virtual DbSet<FreeSchoolMealsHO> FreeSchoolMealsHO { get; set; }

    public Task<int> SaveChangesAsync()
    {
      return  base.SaveChangesAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EligibilityCheck>().ToTable("FsmCheckEligibility");
        modelBuilder.Entity<EligibilityCheck>()
            .Property(p => p.Status)
            .HasConversion(
            v => v.ToString(),
            v => (CheckEligibilityStatus)Enum.Parse(typeof(CheckEligibilityStatus), v));
        modelBuilder.Entity<EligibilityCheck>()
           .Property(p => p.Type)
           .HasConversion(
           v => v.ToString(),
           v => (CheckEligibilityType)Enum.Parse(typeof(CheckEligibilityType), v));

    }

}
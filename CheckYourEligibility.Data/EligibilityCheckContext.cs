// Ignore Spelling: Fsm

using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;

public class EligibilityCheckContext : DbContext, IEligibilityCheckContext
{
    public EligibilityCheckContext(DbContextOptions<EligibilityCheckContext> options) : base(options)
    {
    }

    public virtual  DbSet<EligibilityCheck> FsmCheckEligibilities { get; set; }
    public virtual DbSet<FreeSchoolMealsHMRC> FreeSchoolMealsHMRC { get; set; }
    public virtual DbSet<FreeSchoolMealsHO> FreeSchoolMealsHO { get; set; }
    public virtual DbSet<School> Schools { get; set; }
    public virtual DbSet<LocalAuthority> LocalAuthorities { get; set; }
    public virtual DbSet<Application> Applications { get; set; }
    public virtual DbSet<CheckYourEligibility.Data.Models.ApplicationStatus> ApplicationStatuses { get; set; }

    public Task<int> SaveChangesAsync()
    {
      return  base.SaveChangesAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EligibilityCheck>().ToTable("EligibilityCheck");
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

        modelBuilder.Entity<School>()
       .HasOne(e => e.LocalAuthority);

        modelBuilder.Entity<Application>()
            .HasIndex(b => b.Reference, "idx_Reference")
            .IsUnique();
    }

}
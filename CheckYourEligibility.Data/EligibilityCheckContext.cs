// Ignore Spelling: Fsm

using CheckYourEligibility.Data.Models;
using Microsoft.EntityFrameworkCore;

public class EligibilityCheckContext : DbContext, IEligibilityCheckContext
{
    public EligibilityCheckContext(DbContextOptions<EligibilityCheckContext> options) : base(options)
    {
    }

    public virtual  DbSet<FsmCheckEligibility> FsmCheckEligibilities { get; set; }
    public virtual DbSet<FreeSchoolMealsHMRC> FreeSchoolMealsHMRC { get; set; }
    public virtual DbSet<FreeSchoolMealsHO> FreeSchoolMealsHO { get; set; }

    public void SaveChangesAsync()
    {
        base.SaveChangesAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FsmCheckEligibility>().ToTable("FsmCheckEligibility");
        modelBuilder.Entity<FsmCheckEligibility>()
            .Property(p => p.Status)
            .HasConversion(
            v => v.ToString(),
            v => (FsmCheckEligibilityStatus)Enum.Parse(typeof(FsmCheckEligibilityStatus), v));
            
    }

}
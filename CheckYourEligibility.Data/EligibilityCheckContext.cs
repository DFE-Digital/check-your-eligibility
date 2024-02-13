using CheckYourEligibility.Data.Models;
using Microsoft.EntityFrameworkCore;

public class EligibilityCheckContext : DbContext, IEligibilityCheckContext
{
    public EligibilityCheckContext()
    {
    }

    public EligibilityCheckContext(DbContextOptions<EligibilityCheckContext> options) : base(options)
    {
    }

    public virtual  DbSet<Course> Courses { get; set; }
    public virtual DbSet<Enrollment> Enrollments { get; set; }
    public virtual DbSet<Student> Students { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>().ToTable("Course");
        modelBuilder.Entity<Enrollment>().ToTable("Enrollment");
        modelBuilder.Entity<Student>().ToTable("Student");
    }
}
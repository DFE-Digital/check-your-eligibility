using CheckYourEligibility.Data.Models;
using Microsoft.EntityFrameworkCore;

public interface IEligibilityCheckContext
{
    DbSet<Course> Courses { get; set; }
    DbSet<Enrollment> Enrollments { get; set; }
    DbSet<Student> Students { get; set; }
}
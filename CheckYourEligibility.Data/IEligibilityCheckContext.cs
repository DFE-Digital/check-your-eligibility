using CheckYourEligibility.Data.Models;
using Microsoft.EntityFrameworkCore;

public interface IEligibilityCheckContext
{
    DbSet<FsmCheckEligibility> FsmCheckEligibilities { get; set; }

    
    void SaveChangesAsync();
}
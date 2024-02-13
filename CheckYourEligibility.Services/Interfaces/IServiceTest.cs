using CheckYourEligibility.Data.Models;

namespace CheckYourEligibility.Services.Interfaces
{
    public interface IServiceTest
    {
        Task<List<Student>> OnGetAsync();
    }
}
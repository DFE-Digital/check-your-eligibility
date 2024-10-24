// Ignore Spelling: Fsm

using CheckYourEligibility.Domain.Requests;

namespace CheckYourEligibility.Services.Interfaces
{
    public interface IApiUsers
    {
        Task Create(SystemUserData data);
        Task Delete(string userName);
        Task<IEnumerable<SystemUserData>> GetAllUsers();
        Task<SystemUserData> ValidateUser(SystemUserData login);
    }
}
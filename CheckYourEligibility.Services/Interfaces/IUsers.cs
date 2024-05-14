// Ignore Spelling: Fsm

using CheckYourEligibility.Domain.Requests;

namespace CheckYourEligibility.Services.Interfaces
{
    public interface IUsers
    {
        Task<string> Create(UserData data);
    }
}
using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using System.Threading.Tasks;

namespace CheckYourEligibility.WebApp.UseCases
{
    /// <summary>
    /// Interface for creating or updating a user.
    /// </summary>
    public interface ICreateOrUpdateUserUseCase
    {
        /// <summary>
        /// Execute the use case.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<UserSaveItemResponse> Execute(UserCreateRequest model);
    }
    public class CreateOrUpdateUserUseCase : ICreateOrUpdateUserUseCase
    {
        private readonly IUsers _userService;
        private readonly IAudit _auditService;

        public CreateOrUpdateUserUseCase(IUsers userService, IAudit auditService)
        {
            _userService = Guard.Against.Null(userService);
            _auditService = Guard.Against.Null(auditService);
        }

        public async Task<UserSaveItemResponse> Execute(UserCreateRequest model)
        {
            var response = await _userService.Create(model.Data);
            
            await _auditService.CreateAuditEntry(AuditType.User, response);
            
            return new UserSaveItemResponse { Data = response };
        }
    }
}
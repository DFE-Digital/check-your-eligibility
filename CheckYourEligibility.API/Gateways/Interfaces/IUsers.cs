using CheckYourEligibility.API.Boundary.Requests;
using CheckYourEligibility.API.Domain;
using CheckYourEligibility.API.Domain.Enums;

namespace CheckYourEligibility.API.Gateways.Interfaces;

public interface IUsers
{
    Task<string> CreateOrUpdateFSMParentUser(UserCreateRequest request);

    Task<string> CreateOrUpdateUser(UserCreateRequest request);

    Task<IList<UserRole>> GetUserRoles(string userId);

    Task<UserRole> AddUserRole(string userId, UserRoleName roleName);

    Task<bool> RemoveUserRole(string userId, UserRoleName roleName);

    Task<(IList<User> Users, int TotalRecords)> SearchUsers(string? search, int pageNumber, int pageSize);

    Task<(User User, IList<UserRole> Roles)> GetUser(string userId);

    Task UpdateUserEnabled(string userId, bool isEnabled);
}
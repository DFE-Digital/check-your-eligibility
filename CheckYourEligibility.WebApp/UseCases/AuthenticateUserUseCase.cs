using Ardalis.GuardClauses;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CheckYourEligibility.WebApp.UseCases
{
    /// <summary>
    /// Interface for authenticating a user.
    /// </summary>
    public interface IAuthenticateUserUseCase
    {
        /// <summary>
        /// Execute the use case.
        /// </summary>
        /// <param name="credentials">Client credentials</param>
        /// <param name="jwtConfig">JWT configuration</param>
        /// <returns>JWT auth response with token</returns>
        Task<JwtAuthResponse> Execute(SystemUser credentials, JwtConfig jwtConfig);
    }

    /// <summary>
    /// Use case for authenticating a user using OAuth2 standards.
    /// </summary>
    public class AuthenticateUserUseCase : IAuthenticateUserUseCase
    {
        private readonly IAudit _auditService;

        /// <summary>
        /// Constructor for the AuthenticateUserUseCase.
        /// </summary>
        /// <param name="auditService">Audit service for logging authentication attempts</param>
        public AuthenticateUserUseCase(IAudit auditService)
        {
            _auditService = Guard.Against.Null(auditService);
        }

        /// <summary>
        /// Execute the use case to authenticate clients using client_id and client_secret.
        /// </summary>
        /// <param name="credentials">Client credentials</param>
        /// <param name="jwtConfig">JWT configuration</param>
        /// <returns>JWT authentication response or null if authentication fails</returns>
        public async Task<JwtAuthResponse> Execute(SystemUser credentials, JwtConfig jwtConfig)
        {
            // Initialize credentials if not already done
            if (string.IsNullOrEmpty(credentials.Identifier) || string.IsNullOrEmpty(credentials.Secret))
            {
                credentials.InitializeCredentials();
            }

            var client = AuthenticateClient(credentials.Identifier, credentials.Secret, jwtConfig.ExpectedSecret, credentials.Scope);
            if (client == null)
            {
                return null;
            }

            var tokenString = GenerateJSONWebToken(client, jwtConfig, out var expires);
            if (string.IsNullOrEmpty(tokenString))
            {
                return null;
            }

            var auditData = _auditService.AuditDataGet(Domain.Enums.AuditType.User, credentials.Identifier);
            if (auditData != null)
            {
                try
                {
                    await _auditService.AuditAdd(auditData);
                }
                catch (Exception ex)
                {
                    // continue
                    Console.WriteLine($"Audit log error: {ex.Message}");
                }
            }

            return new JwtAuthResponse { Token = tokenString, Expires = expires };
        }

        private static SystemUser AuthenticateClient(string identifier, string secret, string expectedSecret, string scope = "default")
        {
            if (!string.IsNullOrEmpty(identifier) && secret == expectedSecret && !string.IsNullOrEmpty(expectedSecret))
            {
                return new SystemUser
                {
                    Identifier = identifier,
                    Scope = scope
                };
            }
            return null;
        }

        private static bool ValidateScopes(string requestedScopes, string allowedScopes)
        {
            if (string.IsNullOrEmpty(requestedScopes))
            {
                return true; // No scopes were requested, so it's valid
            }

            if (string.IsNullOrEmpty(allowedScopes))
            {
                return false; // Scopes were requested but client has no allowed scopes
            }

            var requestedScopesList = requestedScopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var allowedScopesList = allowedScopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Check if each requested scope is in the allowed scopes list
            return requestedScopesList.All(scope => allowedScopesList.Contains(scope));
        }

        private static string GenerateJSONWebToken(SystemUser client, JwtConfig jwtConfig, out DateTime expires)
        {
            try
            {
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                // Validate scopes if both requested and expected scopes exist
                if (!string.IsNullOrEmpty(client.Scope) && !string.IsNullOrEmpty(jwtConfig.AllowedScopes) &&
                    !ValidateScopes(client.Scope, jwtConfig.AllowedScopes))
                {
                    expires = DateTime.MinValue;
                    return null; // Invalid scopes
                }

                var claimsList = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, client.Identifier),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                // Only include scope claim if scope was provided
                if (!string.IsNullOrEmpty(client.Scope) && client.Scope != "default")
                {
                    claimsList.Add(new Claim("scope", client.Scope));
                }

                expires = DateTime.UtcNow.AddMinutes(120);
                var token = new JwtSecurityToken(
                    issuer: jwtConfig.Issuer,
                    audience: jwtConfig.Issuer,
                    claims: claimsList,
                    expires: expires,
                    signingCredentials: credentials);

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception)
            {
                expires = DateTime.MinValue;
                return null;
            }
        }
    }
}
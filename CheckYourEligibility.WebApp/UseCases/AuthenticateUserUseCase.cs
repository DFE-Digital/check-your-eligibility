using Ardalis.GuardClauses;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.Extensions.Configuration;
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
        /// <param name="login"></param>
        /// <param name="jwtConfig"></param>
        /// <returns></returns>
        Task<JwtAuthResponse> Execute(SystemUser login, JwtConfig jwtConfig);
    }

    /// <summary>
    /// Use case for authenticating a user.
    /// </summary>
    public class AuthenticateUserUseCase : IAuthenticateUserUseCase
    {
        private readonly IAudit _auditService;

        /// <summary>
        /// Constructor for the AuthenticateUserUseCase.
        /// </summary>
        /// <param name="auditService"></param>
        public AuthenticateUserUseCase(IAudit auditService)
        {
            _auditService = Guard.Against.Null(auditService);
        }

        /// <summary>
        /// Execute the use case.
        /// </summary>
        /// <param name="login"></param>
        /// <param name="jwtConfig"></param>
        /// <returns></returns>
        public async Task<JwtAuthResponse> Execute(SystemUser login, JwtConfig jwtConfig)
        {
            var user = AuthenticateUser(login, jwtConfig.UserPassword);
            if (user == null)
            {
                return null;
            }

            var tokenString = GenerateJSONWebToken(user, jwtConfig, out var expires);
            if (string.IsNullOrEmpty(tokenString))
            {
                return null;
            }
            var auditData = _auditService.AuditDataGet(Domain.Enums.AuditType.User, login.Username);
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

        private static SystemUser? AuthenticateUser(SystemUser login, string password)
        {
            if (login.Password == password && !string.IsNullOrEmpty(password))
            {
                return new SystemUser { Username = login.Username };
            }
            return null;
        }

        private static string? GenerateJSONWebToken(SystemUser userInfo, JwtConfig jwtConfig, out DateTime expires)
        {
            try
            {
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Sub, userInfo.Username),
            new Claim("EcsApi", "apiCustomClaim"),
            new Claim(JwtRegisteredClaimNames.Email, "ecs@ecs.com"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

                expires = DateTime.UtcNow.AddMinutes(120);
                var token = new JwtSecurityToken(jwtConfig.Issuer,
                    jwtConfig.Issuer,
                    claims,
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
using Ardalis.GuardClauses;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Exceptions;
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
        /// Prepares the JWT configuration and authenticates the user.
        /// </summary>
        /// <param name="credentials">Client credentials</param>
        /// <param name="configuration">Application configuration</param>
        /// <returns>JWT auth response with token</returns>
        Task<JwtAuthResponse> AuthenticateUser(SystemUser credentials);
    }

    /// <summary>
    /// Use case for authenticating a user using OAuth2 standards.
    /// </summary>
    public class AuthenticateUserUseCase : IAuthenticateUserUseCase
    {
        private readonly IAudit _auditService;
        private readonly ILogger<AuthenticateUserUseCase> _logger;
        private readonly JwtSettings _jwtSettings;

        /// <summary>
        /// Constructor for the AuthenticateUserUseCase.
        /// </summary>
        /// <param name="auditService">Audit service for logging authentication attempts</param>
        /// <param name="logger">Logger service</param>
        public AuthenticateUserUseCase(IAudit auditService, ILogger<AuthenticateUserUseCase> logger, JwtSettings jwtSettings)
        {
            _auditService = Guard.Against.Null(auditService);
            _logger = Guard.Against.Null(logger);
            _jwtSettings = Guard.Against.Null(jwtSettings);
        }

        /// <summary>
        /// Prepares the JWT configuration and authenticates the user.
        /// </summary>
        /// <param name="credentials">Client credentials</param>
        /// <param name="configuration">Application configuration</param>
        /// <returns>JWT auth response with token</returns>
        /// <exception cref="AuthenticationException">Thrown when authentication fails</exception>
        public async Task<JwtAuthResponse> AuthenticateUser(SystemUser credentials)
        {
            if (!credentials.IsValidGrantType())
            {
                _logger.LogWarning(credentials.GetInvalidGrantTypeMessage().Replace(Environment.NewLine, ""));
            }

            var key = _jwtSettings.Key;
            if (string.IsNullOrEmpty(key))
            {
                _logger.LogError("Jwt:Key is required");
                throw new ServerErrorException("The authorization server is misconfigured. Key is required.");
            }

            var issuer = _jwtSettings.Issuer;
            if (string.IsNullOrEmpty(issuer))
            {
                _logger.LogError("Jwt:Issuer is required");
                throw new ServerErrorException("The authorization server is misconfigured. Issuer is required.");
            }

            credentials.InitializeCredentials();

            if (!credentials.IsValid())
            {
                _logger.LogError("Either client_id/client_secret pair or Username/Password pair must be provided");
                throw new InvalidRequestException("Either client_id/client_secret pair or Username/Password pair must be provided");
            }

            if (!IsValidIdentifier(credentials.Identifier))
            {
                _logger.LogError($"Invalid client or user identifier: {credentials.Identifier.Replace(Environment.NewLine, "")}");
                throw new InvalidClientException("Invalid client or user identifier");
            }

            // Get client secret from configuration
            string? secret = null;
            if (_jwtSettings.Clients.TryGetValue(credentials.Identifier, out ClientSettings? value))
            {
                secret = value?.Secret;
            }
            if (secret == null)
            {
                // Try legacy user config path for backward compatibility
                secret = _jwtSettings.Users[credentials.Identifier];
                if (secret == null)
                {
                    _logger.LogError($"Authentication secret not found for identifier: {credentials.Identifier.Replace(Environment.NewLine, "")}");
                    throw new InvalidClientException("The client authentication failed");
                }
            }

            var jwtConfig = new JwtConfig
            {
                Key = key,
                Issuer = issuer,
                ExpectedSecret = secret
            };

            // Get and validate allowed scopes
            if (!string.IsNullOrEmpty(credentials.client_id) && !string.IsNullOrEmpty(credentials.scope))
            {
                jwtConfig.AllowedScopes = _jwtSettings.Clients[credentials.Identifier]?.Scope;
                if (string.IsNullOrEmpty(jwtConfig.AllowedScopes))
                {
                    _logger.LogError($"Allowed scopes not found for client: {credentials.Identifier.Replace(Environment.NewLine, "")}");
                    throw new InvalidScopeException("Client is not authorized for any scopes");
                }
            }
            return await ExecuteAuthentication(credentials, jwtConfig);
        }

        private bool IsValidIdentifier(string identifier)
        {
            return _jwtSettings.Clients.ContainsKey(identifier) || _jwtSettings.Users.ContainsKey(identifier);
        }

        /// <summary>
        /// Execute the authentication process.
        /// </summary>
        private async Task<JwtAuthResponse> ExecuteAuthentication(SystemUser credentials, JwtConfig jwtConfig)
        {
            if (!ValidateSecret(credentials.Secret, jwtConfig.ExpectedSecret))
            {
                throw new InvalidClientException("Invalid client credentials");
            }

            if (!ValidateScopes(credentials.scope, jwtConfig.AllowedScopes))
            {
                throw new InvalidScopeException("The requested scope is invalid, unknown, or exceeds the scope granted by the resource owner");
            }

            var tokenString = GenerateJSONWebToken(credentials.Identifier, credentials.scope, jwtConfig, out var expires);
            var expiresInSeconds = (int)(expires - DateTime.UtcNow).TotalSeconds;

            if (string.IsNullOrEmpty(tokenString))
            {
                throw new ServerErrorException("The authorization server encountered an unexpected error");
            }

            var auditType = string.IsNullOrEmpty(credentials.client_id) ? Domain.Enums.AuditType.User : Domain.Enums.AuditType.Client;
            await _auditService.CreateAuditEntry(auditType, credentials.Identifier);


            return new JwtAuthResponse { Token = tokenString, expires_in = expiresInSeconds, access_token = tokenString, token_type = "Bearer" };
        }
        private static bool ValidateSecret(string secret, string expectedSecret)
        {
            return !string.IsNullOrEmpty(secret) && !string.IsNullOrEmpty(expectedSecret) && secret == expectedSecret;
        }

        private static bool ValidateScopes(string requestedScopes, string allowedScopes)
        {
            // Empty or default scopes are always valid
            if (string.IsNullOrEmpty(requestedScopes) || requestedScopes == "default")
            {
                return true;
            }

            // If we have specific scope requests but no allowed scopes, it's invalid
            if (string.IsNullOrEmpty(allowedScopes))
            {
                return false;
            }

            var requestedScopesList = requestedScopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var allowedScopesList = allowedScopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Check if each requested scope is in the allowed scopes list
            return requestedScopesList.All(scope => allowedScopesList.Contains(scope));
        }

        private static string GenerateJSONWebToken(string identifier, string scope, JwtConfig jwtConfig, out DateTime expires)
        {
            try
            {
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Key));
                var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var claimsList = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, identifier),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                // Only include scope claim if scope was provided
                if (!string.IsNullOrEmpty(scope) && scope != "default")
                {
                    claimsList.Add(new Claim("scope", scope));
                }

                expires = DateTime.UtcNow.AddMinutes(120);
                var token = new JwtSecurityToken(
                    issuer: jwtConfig.Issuer,
                    audience: jwtConfig.Issuer,
                    claims: claimsList,
                    expires: expires,
                    signingCredentials: signingCredentials);

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
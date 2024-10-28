using Ardalis.GuardClauses;
using CheckYourEligibility.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Eventing.Reader;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CheckYourEligibility.WebApp.Controllers
{
    [ExcludeFromCodeCoverage]
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : Controller
    {
        private IConfiguration _config;
        private readonly ILogger<LoginController> _logger;

        public LoginController(IConfiguration config, ILogger<LoginController> logger)
        {
            _config = config;
            _logger = Guard.Against.Null(logger);
        }
        [AllowAnonymous]
        [HttpPost]
        public IActionResult Login([FromBody] SystemUser login)
        {
            IActionResult response = Unauthorized();
            var user = AuthenticateUser(login);

            if (user != null)
            {
                var tokenString = GenerateJSONWebToken(user, out var expires);
                response = Ok(new JwtAuthResponse{ Token = tokenString, Expires = expires });
               _logger.LogInformation($"{login.Username} authenticated");
            }
            else
            {
                _logger.LogError($"{login.Username} InvalidUser");
            }

            return response;
        }
        private string GenerateJSONWebToken(Domain.SystemUser userInfo, out DateTime  expires)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, userInfo.Username),
                new Claim("EcsApi", "apiCustomClaim"),
                new Claim(JwtRegisteredClaimNames.Email, "ecs@ecs.com"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            expires = DateTime.UtcNow.AddMinutes(120);
            var token = new JwtSecurityToken(_config["Jwt:Issuer"],
              _config["Jwt:Issuer"],
              claims,
              expires: expires,
              signingCredentials: credentials);
           
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private SystemUser AuthenticateUser(SystemUser login)
        {
            //Validate the User Credentials
            var password = _config.GetSection($"Jwt:Users:{login.Username}").Get<string>();

            if (login.Password == password && !password.IsNullOrEmpty()) {
                return new SystemUser { Username = login.Username };
            }
            return null;
        }
    }
}

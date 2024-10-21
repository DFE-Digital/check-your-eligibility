using Ardalis.GuardClauses;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Constants;
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
        private readonly IApiUsers _service;

        public  LoginController(IConfiguration config, ILogger<LoginController> logger, IApiUsers apiUsers)
        {
            _logger = Guard.Against.Null(logger);
            _service = Guard.Against.Null(apiUsers);
            _config = Guard.Against.Null(config);
            

            //try
            //{

            //    _users = JsonConvert.DeserializeObject<List<SystemUserData>>(_config["Jwt:Users"]);
            //}
            //catch (Exception)
            //{
            //    _users = _config.GetSection("Jwt:Users").Get<List<SystemUserData>>();
            //}
            
           
        }
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] SystemUserData login)
        {
            IActionResult response = Unauthorized();
            try
            {
                var user = await AuthenticateUser(login);

                if (user != null)
                {
                    var tokenString = GenerateJSONWebToken(user, out var expires);
                    response = Ok(new JwtAuthResponse { Token = tokenString, Expires = expires });
                    _logger.LogInformation($"{login.UserName} authenticated");
                }
                else
                {
                    _logger.LogError($"{login.UserName} InvalidUser");
                }

                return response;
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, ex.Message);
                return StatusCode(500);
            }
        }
        private string GenerateJSONWebToken(SystemUserData userInfo, out DateTime  expires)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[] {
               // new Claim(ClaimTypes.Role, ApiAccessRoles.Internal),
                new Claim(ClaimTypes.Role, ApiAccessRoles.External),

                new Claim(JwtRegisteredClaimNames.Sub, userInfo.Username),
                new Claim("EceApi", "apiCustomClaim"),
                new Claim(JwtRegisteredClaimNames.Email, "ece@ece.com"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };
            foreach (var role in userInfo.Roles) {
                claims.Append(
                new Claim(ClaimTypes.Role, role));
            }

            expires = DateTime.UtcNow.AddMinutes(120);
            var token = new JwtSecurityToken(_config["Jwt:Issuer"],
              _config["Jwt:Issuer"],
              claims,     
              expires: expires,
              signingCredentials: credentials);
           
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<SystemUserData> AuthenticateUser(SystemUserData login)
        {
            //Validate the User Credentials
            return await _service.ValidateUser(login);
        }
    }
}

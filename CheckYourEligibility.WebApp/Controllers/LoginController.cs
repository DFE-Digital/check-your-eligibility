﻿using Ardalis.GuardClauses;
using CheckYourEligibility.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CheckYourEligibility.WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : Controller
    {
        private IConfiguration _config;
        private List<SystemUser> _users;
        private readonly ILogger<LoginController> _logger;

        public LoginController(IConfiguration config)
        {
            _config = config;
            _users = JsonConvert.DeserializeObject<List<SystemUser>>(_config["Jwt:Users"]); 
            if (logger == null)
            {
                throw new Exception("logger is null");
            }
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

            return response;
        }
        private string GenerateJSONWebToken(Domain.SystemUser userInfo, out DateTime  expires)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, userInfo.Username),
                new Claim("EcsApi", "apiCustomClaim"),
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
            if (_users.FirstOrDefault(x=>x.Username == login.Username && x.Password == login.Password) != null) {
                return new SystemUser { Username = login.Username };
            }
            return null;
        }
    }
}

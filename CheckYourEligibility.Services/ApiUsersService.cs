using Ardalis.GuardClauses;
using AutoMapper;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace CheckYourEligibility.Services
{
    public class ApiUsersService : BaseService, IApiUsers
    {
      
        private readonly ILogger _logger;
        protected readonly IMapper _mapper;
        private readonly IEligibilityCheckContext _db;
        private IConfiguration _config;
        private List<SystemUserData> _users;
        public ApiUsersService(ILoggerFactory logger, IEligibilityCheckContext dbContext, IMapper mapper, IConfiguration configuration) : base()
        {
            _logger = logger.CreateLogger("UsersService");
            _db = Guard.Against.Null(dbContext);
            _mapper = Guard.Against.Null(mapper);
            _config = Guard.Against.Null(configuration); 
            //try
            //{
            //    _users = JsonConvert.DeserializeObject<List<SystemUserData>>(_config["Jwt:Users"]);
            //}
            //catch (Exception)
            //{
            //    _users = _config.GetSection("Jwt:Users").Get<List<SystemUserData>>();
            //}

        }

        /// <summary>
        /// Creates a user, not this can not be tested on existing users exception
        /// due to limitations on in memory db
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task Create(SystemUserData data)
        {
            var item = _mapper.Map<SystemUser>(data);
            item.Password = GetHash(data);

            await _db.SystemUsers.AddAsync(item);
            await _db.SaveChangesAsync();

        }

        public async Task Delete(string userName)
        {
           var item = _db.SystemUsers.First(x=>x.UserName == userName);
            _db.SystemUsers.Remove(item);
            _db.SaveChanges();
        }

        public async Task<IEnumerable<SystemUserData>> GetAllUsers()
        {
            
            var results = _db.SystemUsers.ToList();
            var mappedResults = _mapper.Map<IEnumerable<SystemUserData>>(results);
            return mappedResults;
        }

        private async Task InitiliseSystemUsers()
        {
            //This should load JWT:users and pull them in
            var username = _config["Jwt:UserName"];
            var password = _config["Jwt:password"];
            var roles = _config.GetSection("Jwt:Roles") as List<string>;
            await Create(new SystemUserData { UserName = username,Password = password, Roles = roles});
        }

        public async Task<SystemUserData> ValidateUser(SystemUserData login)
        {
            //try
            //{
                if (!_db.SystemUsers.Any())
                {
                    await InitiliseSystemUsers();
                }
                var encodePS = GetHash(login);
                var results = await _db.SystemUsers.FirstOrDefaultAsync(x => x.UserName == login.UserName && x.Password == encodePS);
                if (results != null)
                {
                    var mappedResults = _mapper.Map<SystemUserData>(results);
                    return mappedResults;
                }
                return null;
            //}
            //catch (Exception)
            //{

            //    throw;
            //}
           
        }

        private string GetHash(SystemUserData item)
        {
            var key = item.Password.ToUpper();
            var input = $"{item.UserName.ToUpper()}{key}{item.Password}";
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var inputHash = SHA256.HashData(inputBytes);
            return Convert.ToHexString(inputHash);
        }

    }
}


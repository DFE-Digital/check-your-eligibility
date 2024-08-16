using Ardalis.GuardClauses;
using AutoMapper;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace CheckYourEligibility.Services
{
    public class UsersService : BaseService, IUsers
    {
      
        private readonly ILogger _logger;
        protected readonly IMapper _mapper;
        private readonly IEligibilityCheckContext _db;
        public UsersService(ILoggerFactory logger, IEligibilityCheckContext dbContext, IMapper mapper) : base()
        {
            _logger = logger.CreateLogger("UsersService");
            _db = Guard.Against.Null(dbContext);
            _mapper = Guard.Against.Null(mapper);
        }

        /// <summary>
        /// Creates a user, not this can not be tested on existing users exception
        /// due to limitations on in memory db
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<string> Create(UserData data)
        {

            var existingUser = _db.Users.FirstOrDefault(x => x.Email == data.Email && x.Reference == data.Reference);
            if (existingUser != null)
            {
                return existingUser.UserID;
            }


            var item = _mapper.Map<User>(data);
            item.UserID = Guid.NewGuid().ToString();

            await _db.Users.AddAsync(item);
            await _db.SaveChangesAsync();

            return item.UserID;
        }


    }
}


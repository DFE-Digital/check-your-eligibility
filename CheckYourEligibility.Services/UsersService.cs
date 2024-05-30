using Ardalis.GuardClauses;
using AutoMapper;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
            try
            {
                var item = _mapper.Map<User>(data);
                item.UserID = Guid.NewGuid().ToString();

                await _db.Users.AddAsync(item);
                await _db.SaveChangesAsync();

                return item.UserID;
            }
            catch (DbUpdateException dbu)
            {

               return  GetRecord(data, dbu);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Db post user");
                throw;
            }
        }

        [ExcludeFromCodeCoverage]
        private string GetRecord(UserData data, DbUpdateException dbu)
        {
            if (dbu.InnerException.Message.StartsWith($"Cannot insert duplicate key row in object 'dbo.Users' with unique index 'IX_Users_Email_Reference'."))
            {
                var existingUser = _db.Users.First(x => x.Email == data.Email && x.Reference == data.Reference);
                return existingUser.UserID;
            }
            _logger.LogError(dbu, "Db find user");
            throw new Exception($"Unable to find user record");
        }

    }
}


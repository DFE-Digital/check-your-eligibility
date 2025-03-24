﻿using AutoMapper;
using CheckYourEligibility.API.Domain;
using CheckYourEligibility.API.Boundary.Requests;
using CheckYourEligibility.API.Gateways.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace CheckYourEligibility.API.Gateways
{
    public class UsersGateway : BaseGateway, IUsers
    {
      
        private readonly ILogger _logger;
        protected readonly IMapper _mapper;
        private readonly IEligibilityCheckContext _db;
        public UsersGateway(ILoggerFactory logger, IEligibilityCheckContext dbContext, IMapper mapper) : base()
        {
            _logger = logger.CreateLogger("UsersService");
            _db = dbContext;
            _mapper = mapper;
        }

        /// <summary>
        /// Creates a user, not this can not be tested on existing users exception
        /// due to limitations on in memory db
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<string> Create(UserData data)
        {

            var existingUser = _db.Users.FirstOrDefault(x => x.Email.ToLower() == data.Email.ToLower() && x.Reference.ToLower() == data.Reference.ToLower());
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


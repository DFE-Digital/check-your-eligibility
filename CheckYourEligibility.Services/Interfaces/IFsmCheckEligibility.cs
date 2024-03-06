﻿// Ignore Spelling: Fsm

using CheckYourEligibility.Data.Enums;
using CheckYourEligibility.Data.Models;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;

namespace CheckYourEligibility.Services.Interfaces
{
    public interface IFsmCheckEligibility
    {
        Task<CheckEligibilityItemFsm?> GetItem(string guid);
        Task<CheckEligibilityStatus?> GetStatus(string guid);
        Task<string> PostCheck(CheckEligibilityRequestDataFsm data);
        Task<CheckEligibilityStatus?> ProcessCheck(string guid);
    }
}
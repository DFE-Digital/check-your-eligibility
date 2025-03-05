using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Requests.DWP;
using CheckYourEligibility.Domain.Responses.DWP;
using CheckYourEligibility.Services.Interfaces;

namespace CheckYourEligibility.WebApp.UseCases
{
    /// <summary>
    /// Interface for matching a citizen.
    /// </summary>
    public interface IMatchCitizenUseCase
    {
        /// <summary>
        /// Execute the use case.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<DwpMatchResponse> Execute(CitizenMatchRequest model);
    }

    /// <summary>
    /// Use case for matching a citizen.
    /// </summary>
    public class MatchCitizenUseCase : IMatchCitizenUseCase
    {
        private readonly ICheckEligibility _service;

        public MatchCitizenUseCase(ICheckEligibility service)
        {
            _service = Guard.Against.Null(service);
        }

        public async Task<DwpMatchResponse> Execute(CitizenMatchRequest model)
        {
            // Implement the logic for matching a citizen here
            if (model?.Data?.Attributes?.LastName.ToUpper() == MogDWPValues.validCitizenSurnameEligible.ToUpper()
                || model?.Data?.Attributes?.LastName.ToUpper() == MogDWPValues.validCitizenSurnameNotEligible.ToUpper())
            {
                return new DwpMatchResponse()
                {
                    Data = new DwpMatchResponse.DwpResponse_Data
                    {
                        Id = model?.Data?.Attributes?.LastName.ToUpper() == MogDWPValues.validCitizenSurnameEligible.ToUpper() ? MogDWPValues.validCitizenEligibleGuid : MogDWPValues.validCitizenNotEligibleGuid,
                        Type = "MatchResult",
                        Attributes = new DwpMatchResponse.DwpResponse_Attributes { MatchingScenario = "FSM" }
                    },
                    Jsonapi = new DwpMatchResponse.DwpResponse_Jsonapi { Version = "2.0" }
                };
            }
            else if (model?.Data?.Attributes?.LastName.ToUpper() == MogDWPValues.validCitizenSurnameDuplicatesFound.ToUpper())
            {
                throw new InvalidOperationException("Duplicates found");
            }
            else
            {
                return null;
            }
        }
    }
}
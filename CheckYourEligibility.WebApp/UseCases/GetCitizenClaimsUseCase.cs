using CheckYourEligibility.Domain.Constants;
using CheckYourEligibility.Domain.Responses;
using Newtonsoft.Json;

namespace CheckYourEligibility.WebApp.UseCases
{
    /// <summary>
    /// Interface for getting citizen claims.
    /// </summary>
    public interface IGetCitizenClaimsUseCase
    {
        /// <summary>
        /// Execute the use case.
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="benefitType"></param>
        /// <returns></returns>
        Task<DwpClaimsResponse> Execute(string guid, string benefitType);
    }

    /// <summary>
    /// Use case for getting citizen claims.
    /// </summary>
    public class GetCitizenClaimsUseCase : IGetCitizenClaimsUseCase
    {
        public async Task<DwpClaimsResponse> Execute(string guid, string benefitType)
        {
            if (guid == MogDWPValues.validCitizenEligibleGuid)
            {
                var response = JsonConvert.DeserializeObject<DwpClaimsResponse>(GetClaimResponse(benefitType));
                return response;
            }
            else if (guid == MogDWPValues.validCitizenNotEligibleGuid)
            {
                return null;
            }
            else
            {
                throw new ArgumentException("Invalid GUID");
            }
        }

        private string GetClaimResponse(string benefitType)
        {
            Enum.TryParse(benefitType, out DwpBenefitType dwpBenefitType);

            switch (dwpBenefitType)
            {
                case DwpBenefitType.employment_support_allowance_income_based:
                // return Properties.Resources.;
                case DwpBenefitType.job_seekers_allowance_income_based:
                // return Properties.Resources.;
                case DwpBenefitType.pensions_credit:
                    return Properties.Resources.DwpClaims_pensions_credit;
                case DwpBenefitType.income_support:
                    return Properties.Resources.DwpClaims_income_support;
                case DwpBenefitType.universal_credit:
                    return Properties.Resources.DwpClaims_universal_credit;
                default:
                    return Properties.Resources.DwpClaims_all;
            }
        }
    }
}
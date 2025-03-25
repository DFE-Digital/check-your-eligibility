using CheckYourEligibility.API.Boundary.Responses;
using CheckYourEligibility.API.Domain.Constants;
using CheckYourEligibility.API.Properties;
using Newtonsoft.Json;

namespace CheckYourEligibility.API.UseCases;

/// <summary>
///     Interface for getting citizen claims.
/// </summary>
public interface IGetCitizenClaimsUseCase
{
    /// <summary>
    ///     Execute the use case.
    /// </summary>
    /// <param name="guid"></param>
    /// <param name="benefitType"></param>
    /// <returns></returns>
    Task<DwpClaimsResponse> Execute(string guid, string benefitType);
}

/// <summary>
///     Use case for getting citizen claims.
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

        if (guid == MogDWPValues.validCitizenNotEligibleGuid) return null;

        throw new ArgumentException("Invalid GUID");
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
                return Resources.DwpClaims_pensions_credit;
            case DwpBenefitType.income_support:
                return Resources.DwpClaims_income_support;
            case DwpBenefitType.universal_credit:
                return Resources.DwpClaims_universal_credit;
            default:
                return Resources.DwpClaims_all;
        }
    }
}
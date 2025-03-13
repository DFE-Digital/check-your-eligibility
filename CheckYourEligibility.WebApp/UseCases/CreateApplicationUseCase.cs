using Ardalis.GuardClauses;
using CheckYourEligibility.Domain.Enums;
using CheckYourEligibility.Domain.Requests;
using CheckYourEligibility.Domain.Responses;
using CheckYourEligibility.Services.Interfaces;
using FeatureManagement.Domain.Validation;
using FluentValidation;
using System.Threading.Tasks;

namespace CheckYourEligibility.WebApp.UseCases
{
    public interface ICreateApplicationUseCase
    {
        Task<ApplicationSaveItemResponse> Execute(ApplicationRequest model);
    }

    public class CreateApplicationUseCase : ICreateApplicationUseCase
    {
        private readonly IApplication _applicationService;
        private readonly IAudit _auditService;

        public CreateApplicationUseCase(IApplication applicationService, IAudit auditService)
        {
            _applicationService = Guard.Against.Null(applicationService);
            _auditService = Guard.Against.Null(auditService);
        }

        public async Task<ApplicationSaveItemResponse> Execute(ApplicationRequest model)
        {
            if (model == null || model.Data == null)
            {
                throw new ValidationException("Invalid request, data is required");
            }
            if (model.Data.Type == Domain.Enums.CheckEligibilityType.None)
            {
                throw new ValidationException($"Invalid request, Valid Type is required: {model.Data.Type}");
            }

            model.Data.ParentNationalInsuranceNumber = model.Data.ParentNationalInsuranceNumber?.ToUpper();
            model.Data.ParentNationalAsylumSeekerServiceNumber = model.Data.ParentNationalAsylumSeekerServiceNumber?.ToUpper();

            var validator = new ApplicationRequestValidator();
            var validationResults = validator.Validate(model);

            if (!validationResults.IsValid)
            {
                throw new ValidationException(validationResults.ToString());
            }

            var response = await _applicationService.PostApplication(model.Data);
            if (response != null)
            {
                await _auditService.CreateAuditEntry(AuditType.Application, response.Id);
            }


            return new ApplicationSaveItemResponse
            {
                Data = response,
                Links = new ApplicationResponseLinks
                {
                    get_Application = $"{Domain.Constants.ApplicationLinks.GetLinkApplication}{response?.Id}"
                }
            };
        }
    }
}
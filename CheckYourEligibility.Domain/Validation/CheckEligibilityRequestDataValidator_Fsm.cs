// Ignore Spelling: Validator

namespace FeatureManagement.Domain.Validation
{
    using CheckYourEligibility.Domain.Constants.ErrorMessages;
    using CheckYourEligibility.Domain.Requests;
    using CheckYourEligibility.Domain.Validation;
    using FluentValidation;

    public class CheckEligibilityRequestDataValidator_Fsm : AbstractValidator<CheckEligibilityRequestData_Fsm>
    {
        public CheckEligibilityRequestDataValidator_Fsm()
        {
            RuleFor(x => x.LastName)
               .NotEmpty().WithMessage(ApplicationValidationMessages.LastName);

            RuleFor(x => x.DateOfBirth)
               .NotEmpty()
               .Must(DataValidation.BeAValidDate)
               .WithMessage(ApplicationValidationMessages.DOB);

            When(x => !string.IsNullOrEmpty(x.NationalInsuranceNumber), () =>
            {
                RuleFor(x => x.NationalAsylumSeekerServiceNumber)
                    .Empty()
                    .WithMessage(ApplicationValidationMessages.NI_and_NASS);
                RuleFor(x => x.NationalInsuranceNumber)
                .NotEmpty()
                   .Must(DataValidation.BeAValidNi)
                   .WithMessage(ApplicationValidationMessages.NI);

            }).Otherwise(() =>
            {
                RuleFor(x => x.NationalAsylumSeekerServiceNumber)
                    .NotEmpty()
                   .WithMessage(ApplicationValidationMessages.NI_or_NASS);
            });
        }
    }
}

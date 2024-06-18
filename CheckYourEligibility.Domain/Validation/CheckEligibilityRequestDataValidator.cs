// Ignore Spelling: Validator

namespace FeatureManagement.Domain.Validation
{
    using CheckYourEligibility.Domain.Constants.ErrorMessages;
    using CheckYourEligibility.Domain.Requests;
    using CheckYourEligibility.Domain.Validation;
    using FluentValidation;

    public class CheckEligibilityRequestDataValidator : AbstractValidator<CheckEligibilityRequestDataFsm>
    {
        public CheckEligibilityRequestDataValidator()
        {
            RuleFor(x => x.LastName)
               .NotEmpty().WithMessage(FSM.LastName);

            RuleFor(x => x.DateOfBirth)
               .NotEmpty()
               .Must(DataValidation.BeAValidDate)
               .WithMessage(FSM.DOB);

            When(x => !string.IsNullOrEmpty(x.NationalInsuranceNumber), () =>
            {
                RuleFor(x => x.NationalAsylumSeekerServiceNumber)
                    .Empty()
                    .WithMessage(FSM.NI_and_NASS);
                RuleFor(x => x.NationalInsuranceNumber)
                .NotEmpty()
                   .Must(DataValidation.BeAValidNi)
                   .WithMessage(FSM.NI);

            }).Otherwise(() =>
            {
                RuleFor(x => x.NationalAsylumSeekerServiceNumber)
                    .NotEmpty()
                   .WithMessage(FSM.NI_or_NASS);
            });
        }
    }
}

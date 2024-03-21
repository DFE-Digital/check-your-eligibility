// Ignore Spelling: Validator

namespace FeatureManagement.Domain.Validation
{
    using CheckYourEligibility.Domain.Constants.ErrorMessages;
    using CheckYourEligibility.Domain.Requests;
    using CheckYourEligibility.Domain.Validation;
    using FluentValidation;

    public class CheckEligibilityRequestDataValidator : AbstractValidator<CheckEligibilityRequest>
    {
        public CheckEligibilityRequestDataValidator()
        {

            RuleFor(x => x.Data)
                .NotNull()
                .WithMessage("data is required");

            RuleFor(x => x.Data.LastName)
               .NotEmpty().WithMessage(FSM.LastName);

            RuleFor(x => x.Data.DateOfBirth)
               .NotEmpty()
               .Must(DataValidation.BeAValidDate)
               .WithMessage(FSM.DOB);

            When(x => !string.IsNullOrEmpty(x.Data.NationalInsuranceNumber), () =>
            {
                RuleFor(x => x.Data.NationalAsylumSeekerServiceNumber)
                    .Empty()
                    .WithMessage(FSM.NI_and_NASS);
                RuleFor(x => x.Data.NationalInsuranceNumber)
                .NotEmpty()
                   .Must(DataValidation.BeAValidNi)
                   .WithMessage(FSM.NI);

            }).Otherwise(() =>
            {
                RuleFor(x => x.Data.NationalAsylumSeekerServiceNumber)
                    .NotEmpty()
                   .WithMessage(FSM.NI_or_NASS);
            });

        }

       
    }
}

// Ignore Spelling: Validator

namespace FeatureManagement.Domain.Validation
{
    using CheckYourEligibility.Domain.Constants.ErrorMessages;
    using CheckYourEligibility.Domain.Requests;
    using CheckYourEligibility.Domain.Validation;
    using FluentValidation;
    using System.Text.RegularExpressions;

    public class ApplicationRequestFsmValidator : AbstractValidator<ApplicationRequest>
    {
        public ApplicationRequestFsmValidator()
        {

            RuleFor(x => x.Data)
                .NotNull()
                .WithMessage("data is required");

            RuleFor(x => x.Data.ParentFirstName)
               .NotEmpty().WithMessage(FSM.FirstName);
            RuleFor(x => x.Data.ParentLastName)
               .NotEmpty().WithMessage(FSM.LastName);
            RuleFor(x => x.Data.ChildFirstName)
              .NotEmpty().WithMessage(FSM.ChildFirstName);
            RuleFor(x => x.Data.ChildLastName)
               .NotEmpty().WithMessage(FSM.ChildLastName);

            RuleFor(x => x.Data.ParentDateOfBirth)
               .NotEmpty()
               .Must(DataValidation.BeAValidDate)
               .WithMessage(FSM.DOB);
            RuleFor(x => x.Data.ChildDateOfBirth)
               .NotEmpty()
               .Must(DataValidation.BeAValidDate)
               .WithMessage(FSM.ChildDOB);

            When(x => !string.IsNullOrEmpty(x.Data.ParentNationalInsuranceNumber), () =>
            {
                RuleFor(x => x.Data.ParentNationalAsylumSeekerServiceNumber)
                    .Empty()
                    .WithMessage(FSM.NI_and_NASS);
                RuleFor(x => x.Data.ParentNationalInsuranceNumber)
                .NotEmpty()
                   .Must(DataValidation.BeAValidNi)
                   .WithMessage(FSM.NI);

            }).Otherwise(() =>
            {
                RuleFor(x => x.Data.ParentNationalAsylumSeekerServiceNumber)
                    .NotEmpty()
                   .WithMessage(FSM.NI_or_NASS);
            });

        }
    }
}

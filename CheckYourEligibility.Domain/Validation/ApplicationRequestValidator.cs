// Ignore Spelling: Validator

namespace FeatureManagement.Domain.Validation
{
    using CheckYourEligibility.Domain.Constants.ErrorMessages;
    using CheckYourEligibility.Domain.Requests;
    using CheckYourEligibility.Domain.Validation;
    using FluentValidation;

    public class ApplicationRequestValidator : AbstractValidator<ApplicationRequest>
    {
        public ApplicationRequestValidator()
        {

            RuleFor(x => x.Data)
                .NotNull()
                .WithMessage("data is required");

            RuleFor(x => x.Data.ParentFirstName)
               .NotEmpty().WithMessage(ApplicationValidationMessages.FirstName);
            RuleFor(x => x.Data.ParentLastName)
               .NotEmpty().WithMessage(ApplicationValidationMessages.LastName);
            RuleFor(x => x.Data.ChildFirstName)
              .NotEmpty().WithMessage(ApplicationValidationMessages.ChildFirstName);
            RuleFor(x => x.Data.ChildLastName)
               .NotEmpty().WithMessage(ApplicationValidationMessages.ChildLastName);

            RuleFor(x => x.Data.ParentDateOfBirth)
               .NotEmpty()
               .Must(DataValidation.BeAValidDate)
               .WithMessage(ApplicationValidationMessages.DOB);
            RuleFor(x => x.Data.ChildDateOfBirth)
               .NotEmpty()
               .Must(DataValidation.BeAValidDate)
               .WithMessage(ApplicationValidationMessages.ChildDOB);

            When(x => !string.IsNullOrEmpty(x.Data.ParentNationalInsuranceNumber), () =>
            {
                RuleFor(x => x.Data.ParentNationalAsylumSeekerServiceNumber)
                    .Empty()
                    .WithMessage(ApplicationValidationMessages.NI_and_NASS);
                RuleFor(x => x.Data.ParentNationalInsuranceNumber)
                .NotEmpty()
                   .Must(DataValidation.BeAValidNi)
                   .WithMessage(ApplicationValidationMessages.NI);

            }).Otherwise(() =>
            {
                RuleFor(x => x.Data.ParentNationalAsylumSeekerServiceNumber)
                    .NotEmpty()
                   .WithMessage(ApplicationValidationMessages.NI_or_NASS);
            });

        }
    }
}

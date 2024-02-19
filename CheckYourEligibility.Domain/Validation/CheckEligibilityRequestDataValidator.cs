// Ignore Spelling: Validator

namespace FeatureManagement.Domain.Validation
{
    using CheckYourEligibility.Domain.Requests;
    using FluentValidation;
    using System.Text.RegularExpressions;

    public class CheckEligibilityRequestDataValidator : AbstractValidator<CheckEligibilityRequest>
    {
        public CheckEligibilityRequestDataValidator()
        {

            RuleFor(x => x.Data)
                .NotNull()
                .WithMessage("data is required");

            RuleFor(x => x.Data.LastName)
               .NotEmpty().WithMessage("LastName is required");

            RuleFor(x => x.Data.DateOfBirth)
               .NotEmpty()
               .Must(BeAValidDate)
               .WithMessage("Date of birth is required:- (dd/mm/yyyy)");

            When(x => !string.IsNullOrEmpty(x.Data.NationalInsuranceNumber), () =>
            {
                RuleFor(x => x.Data.NationalAsylumSeekerServiceNumber)
                    .Empty()
                    .WithMessage("National Insurance Number or National Asylum Seeker Service Number is required is required, not both");
                RuleFor(x => x.Data.NationalInsuranceNumber)
                .NotEmpty()
                   .Must(BeAValidNi)
                   .WithMessage("Invalid National Insurance Number");

            }).Otherwise(() =>
            {
                RuleFor(x => x.Data.NationalAsylumSeekerServiceNumber)
                    .NotEmpty()
                   .WithMessage("National Insurance Number or National Asylum Seeker Service Number is required");
            });

        }

        private bool BeAValidNi(string value)
        {
            string regexString =
       @"^(?!BG)(?!GB)(?!NK)(?!KN)(?!TN)(?!NT)(?!ZZ)(?:[A-CEGHJ-PR-TW-Z][A-CEGHJ-NPR-TW-Z])(?:\s*\d\s*){6}([A-D]|\s)$";
            Regex rg = new Regex(regexString);
            var res = rg.Match(value);
            return res.Success;
        }

        private bool BeAValidDate(string value)
        {
            string regexString =
       @"^(0?[1-9]|[12][0-9]|3[01])[\/\-](0?[1-9]|1[012])[\/\-]\d{4}$";
            Regex rg = new Regex(regexString);
            var res = rg.Match(value);
            return res.Success;
        }
    }
}

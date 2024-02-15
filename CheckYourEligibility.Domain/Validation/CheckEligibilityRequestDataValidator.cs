namespace FeatureManagement.Domain.Validation
{
    using CheckYourEligibility.Domain.Requests;
    using FluentValidation;
    using FluentValidation.Validators;
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

            When(x => !string.IsNullOrEmpty(x.Data.NiNumber), () =>
            {
                RuleFor(x => x.Data.NASSNumber)
                    .Empty()
                    .WithMessage("National Insurance Number or National Asylum Seeker Service Number is required is required, not both");
                RuleFor(x => x.Data.NiNumber)
                .NotEmpty()
                   .Must(BeAValidNi)
                   .WithMessage("Invalid National Insurance Number");

            }).Otherwise(() =>
            {
                RuleFor(x => x.Data.NASSNumber)
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
            return DateTime.TryParse(value, out _);
        }
    }
}

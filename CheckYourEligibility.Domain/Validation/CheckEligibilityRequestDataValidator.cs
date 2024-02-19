// Ignore Spelling: Validator

namespace FeatureManagement.Domain.Validation
{
    using CheckYourEligibility.Domain.Constants.ErrorMessages;
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
               .NotEmpty().WithMessage(FSM.LastName);

            RuleFor(x => x.Data.DateOfBirth)
               .NotEmpty()
               .Must(BeAValidDate)
               .WithMessage(FSM.DOB);

            When(x => !string.IsNullOrEmpty(x.Data.NationalInsuranceNumber), () =>
            {
                RuleFor(x => x.Data.NationalAsylumSeekerServiceNumber)
                    .Empty()
                    .WithMessage(FSM.NI_and_NASS);
                RuleFor(x => x.Data.NationalInsuranceNumber)
                .NotEmpty()
                   .Must(BeAValidNi)
                   .WithMessage(FSM.NI);

            }).Otherwise(() =>
            {
                RuleFor(x => x.Data.NationalAsylumSeekerServiceNumber)
                    .NotEmpty()
                   .WithMessage(FSM.NI_or_NASS);
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

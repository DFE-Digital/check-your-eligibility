namespace FeatureManagement.Domain.Validation
{
    using CheckYourEligibility.Domain.Requests;
    using FluentValidation;

    public class CheckEligibilityRequestDataValidator : AbstractValidator<CheckEligibilityRequest>
    {
        public CheckEligibilityRequestDataValidator()
        {

            RuleFor(x => x.Data)
                .NotNull()
                .WithMessage("data is required.");

                RuleFor(x => x.Data.LastName)
                   .NotEmpty();
                RuleFor(x => x.Data.DateOfBirth)
                   .NotEmpty()
                   .Must(BeAValidDate)
                   .WithMessage("Date of birth is required:- (dd/mm/yyyy).");
            RuleFor(customer => customer.Data.NiNumber)
                .NotEmpty()
                .When(customer => string.IsNullOrEmpty(customer.Data.NASSNumber))
                .WithMessage("national asylum seeker service number or ni number is required.");

        }

        private bool BeAValidDate(string value)
        {
            DateTime date;
            return DateTime.TryParse(value, out date);
        }
    }
}

using System.Text.RegularExpressions;

namespace CheckYourEligibility.Domain.Validation
{
    internal static class DataValidation
    {

        internal static bool BeAValidNi(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            string regexString =
       @"^(?!BG)(?!GB)(?!NK)(?!KN)(?!TN)(?!NT)(?!ZZ)(?:[A-CEGHJ-PR-TW-Z][A-CEGHJ-NPR-TW-Z])(?:\s*\d\s*){6}([A-D]|\s)$";
            Regex rg = new Regex(regexString);
            var res = rg.Match(value);
            return res.Success;
        }

        internal static bool BeAValidDate(string value)
        {
            string regexString =
       @"^(0?[1-9]|[12][0-9]|3[01])[\/\-](0?[1-9]|1[012])[\/\-]\d{4}$";
            Regex rg = new Regex(regexString);
            var res = rg.Match(value);
            return res.Success;
        }
    }
}

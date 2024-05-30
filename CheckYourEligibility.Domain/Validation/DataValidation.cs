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
       @"^\d{4}-\d{2}-\d{2}$";
            Regex rg = new Regex(regexString);
            var res = rg.Match(value);
            return res.Success;
        }
    }
}

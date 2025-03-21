using System;
using CheckYourEligibility.Domain.Responses;

namespace CheckYourEligibility.Domain.Exceptions
{
    public class ValidationException : Exception
    {
        public List<Error> Errors;
        public ValidationException(List<Error> errors, string errorDescription)
            : base(errorDescription)
        {
            if (errors == null)
            {
                errors = new List<Error>{new Error(){Title = errorDescription}};
            }
            Errors = errors;
        }
    }
}
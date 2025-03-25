// Ignore Spelling: Fsm

namespace CheckYourEligibility.API.Domain.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException()
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }
}
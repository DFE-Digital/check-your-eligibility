// Ignore Spelling: Fsm

namespace CheckYourEligibility.Domain.Exceptions
{

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
}

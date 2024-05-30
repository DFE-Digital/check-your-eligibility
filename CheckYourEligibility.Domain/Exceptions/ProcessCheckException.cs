// Ignore Spelling: Fsm

namespace CheckYourEligibility.Domain.Exceptions
{

    public class ProcessCheckException : Exception
    {
        public ProcessCheckException()
        {
        }

        public ProcessCheckException(string message)
            : base(message)
        {
        }
    }
}

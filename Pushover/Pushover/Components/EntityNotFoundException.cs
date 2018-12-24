namespace Pushover.Components
{
    using System;

    public class InternalErrorException : Exception
    {
        public InternalErrorException(string message)
            : base(message)
        {
        }
    }
}

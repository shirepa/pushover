namespace Pushover.Components
{
    using System;

    public class BadParametersException : Exception
    {
        public BadParametersException(string message)
           : base(message)
        {
        }
    }
}

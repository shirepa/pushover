namespace Pushover.Components
{
    using System; 

    public class ServerNotResponding : Exception
    {
        public ServerNotResponding(Exception innerException = null) : base("Server not responding", innerException)
        {
        }
    }
}

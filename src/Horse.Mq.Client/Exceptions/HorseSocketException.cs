using System;

namespace Horse.Mq.Client.Exceptions
{
    /// <summary>
    /// Thrown when an error occured on connections
    /// </summary>
    public class HorseSocketException : Exception
    {
        /// <summary>
        /// Created new HorseSocketException
        /// </summary>
        public HorseSocketException(string message) : base(message)
        {
        }
    }
}
using System;
using System.Runtime.Serialization;

namespace ChatBasicApp
{
    [Serializable]
    internal class ParseInputException : Exception
    {
        private const string DefaultMessage = "An error occurred while parsing input.";
        public ParseInputException() : base(DefaultMessage)
        {
           
        }

        public ParseInputException(string message) : base(message)
        {
            
        }

        public ParseInputException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ParseInputException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            
        }
    }
}
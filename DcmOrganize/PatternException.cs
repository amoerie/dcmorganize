using System;
using System.Runtime.Serialization;

namespace DcmOrganize
{
    public class PatternException : Exception
    {
        public PatternException() { }
        protected PatternException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        public PatternException(string? message) : base(message) { }
        public PatternException(string? message, Exception? innerException) : base(message, innerException) { }
    }
}
using System;
using System.Runtime.Serialization;

namespace DcmOrganize
{
    public class DicomTagParserException : Exception
    {
        public DicomTagParserException() { }
        protected DicomTagParserException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        public DicomTagParserException(string? message) : base(message) { }
        public DicomTagParserException(string? message, Exception? innerException) : base(message, innerException) { }
    }
}
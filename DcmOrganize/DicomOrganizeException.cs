using System;
using System.Runtime.Serialization;

namespace DcmOrganize;

internal class DicomOrganizeException : Exception
{
    public DicomOrganizeException() { }
    protected DicomOrganizeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    public DicomOrganizeException(string? message) : base(message) { }
    public DicomOrganizeException(string? message, Exception? innerException) : base(message, innerException) { }
}
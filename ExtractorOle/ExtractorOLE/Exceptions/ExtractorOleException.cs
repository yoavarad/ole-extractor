using System;

namespace ExtractorOLE.Exceptions
{
    public abstract class ExtractorOleException : Exception
    {
        public abstract string Code { get; }
        public abstract int StatusCode { get; }
        public string Detail => Message;
        protected ExtractorOleException(string message) : base(message) { }
    }
}

using System;

namespace DNMPLibrary.Core
{
    public class DNMPException : Exception
    {
        public DNMPException(string message) : base(message) { }
    }
}

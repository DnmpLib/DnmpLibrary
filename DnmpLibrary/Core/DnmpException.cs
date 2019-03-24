using System;

namespace DnmpLibrary.Core
{
    public class DnmpException : Exception
    {
        public DnmpException(string message) : base(message) { }
    }
}

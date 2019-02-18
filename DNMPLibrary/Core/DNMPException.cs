using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNMPLibrary.Core
{
    public class DNMPException : Exception
    {
        public DNMPException(string message) : base(message) { }
    }
}

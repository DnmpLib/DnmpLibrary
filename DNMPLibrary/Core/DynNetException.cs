using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNMPLibrary.Core
{
    public class DynNetException : Exception
    {
        public DynNetException(string message) : base(message) { }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNMPLibrary.Messages.Types
{
    internal interface ITypedMessage
    {
        MessageType GetMessageType();

        byte[] GetBytes();
    }
}

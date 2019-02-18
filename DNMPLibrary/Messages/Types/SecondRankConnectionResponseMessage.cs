using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DNMPLibrary.Client;
using DNMPLibrary.Core;
using DNMPLibrary.Security;

namespace DNMPLibrary.Messages.Types
{
    internal class SecondRankConnectionResponseMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.SecondRankConnectionResponse;

        public SecondRankConnectionResponseMessage() { }

        public byte[] GetBytes() => new byte[0];
    }
}

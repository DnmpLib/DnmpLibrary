using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DNMPLibrary.Messages.Types
{
    internal class ConnectionRequestMessage : ITypedMessage
    {
        public MessageType GetMessageType() => MessageType.ConnectionRequest;

        public byte[] NetworkId;

        public ConnectionRequestMessage(byte[] data)
        {
            NetworkId = data;
        }

        public ConnectionRequestMessage(byte[] networkId, bool packetCreation = false)
        {
            NetworkId = networkId;
        }

        public byte[] GetBytes() => NetworkId;
    }
}

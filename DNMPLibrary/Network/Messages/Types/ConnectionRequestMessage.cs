using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DNMPLibrary.Network.Messages;
using DNMPLibrary.Network.Messages.Types;

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

        public ConnectionRequestMessage(byte[] networkId, bool packetCreation = false) //-V3117
        {
            NetworkId = networkId;
        }

        public byte[] GetBytes() => NetworkId;
    }
}

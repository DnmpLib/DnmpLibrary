using System;
using System.Collections.Concurrent;
using DnmpLibrary.Interaction.Protocol;
using DnmpLibrary.Network.Messages.Types;
using DnmpLibrary.Security.Cryptography.Symmetric;

namespace DnmpLibrary.Core
{
    public class DnmpNode
    {
        public ushort Id { get; internal set; }

        public IEndPoint EndPoint { get; internal set; }

        public ClientFlags Flags { get; internal set; }

        public ushort ParentId { get; internal set; } = 0xFFFF;

        public ushort DirectPing { get; internal set; } = ushort.MaxValue;

        public byte[] CustomData { get; internal set; } = new byte[0];

        public PingPair RedirectPing { get; internal set; } = new PingPair
        {
            Id = 0xFFFF,
            Ping = ushort.MaxValue
        };

        public ISymmetricKey MainKey;

        internal DateTime LastPingSendTime { get; set; } = DateTime.MinValue;

        internal DateTime LastPingReceiveTime { get; set; } = DateTime.Now;

        internal DateTime LastForcePingUpdateTime { get; set; } = DateTime.MinValue;

        internal Guid DisconnectEventGuid { get; set; }

        internal ConcurrentQueue<byte[]> DataMessageQueue { get; set; } = new ConcurrentQueue<byte[]>();

        public long BytesReceived { get; internal set; }

        public long DataBytesReceived { get; internal set; }

        public long BytesSent { get; internal set; }

        public long DataBytesSent { get; internal set; }
    }

    [Flags]
    public enum ClientFlags
    {
        DirectConnectionAvailable = 1,
        SymmetricKeyExchangeInProgress = 2,
        SymmetricKeyExchangeDone = 4
    }
}

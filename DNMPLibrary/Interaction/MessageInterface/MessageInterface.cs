using System;
using System.Threading.Tasks;
using DNMPLibrary.Core;

namespace DNMPLibrary.Interaction.MessageInterface
{
    public abstract class MessageInterface
    {
        public delegate Task<bool> SendCallback(byte[] data, ushort destinationId);
        public SendCallback Send { get; internal set; }

        public delegate Task BroadcastCallback(byte[] data);
        public BroadcastCallback Broadcast { get; internal set; }

        public delegate bool HostExistsCallback(ushort hostId);
        public HostExistsCallback HostExists { get; internal set; }

        public delegate DNMPNode[] GetNodesCallback();
        public GetNodesCallback GetNodes { get; internal set; }

        public class DataMessageEventArgs : EventArgs
        {
            public byte[] Data;
            public ushort SourceId;
            public bool IsBroadcast;
        };

        public virtual async void PacketReceived(object sender, DataMessageEventArgs eventArgs)
        {
            await Task.Delay(0);
        }

        public virtual async void Initialize(ushort newSelfId)
        {
            await Task.Delay(0);
        }

        public virtual ushort GetMaxClientCount()
        {
            return 0xFFFE;
        }
    }
}

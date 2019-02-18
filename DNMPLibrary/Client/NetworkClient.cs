using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using DNMPLibrary.Core;
using DNMPLibrary.Messages;
using DNMPLibrary.Messages.Types;
using DNMPLibrary.Security;
using DNMPLibrary.Util;
using NLog;

namespace DNMPLibrary.Client
{
    internal class NetworkClient : IDisposable
    {
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        private class StateObject
        {
            public readonly byte[] Buffer;
            public EndPoint EndPoint;

            public StateObject(int bufferSize)
            {
                Buffer = new byte[bufferSize];
            }
        }
        
        private readonly RealClient realClient;
        private readonly HashSet<Guid> receivedReliableMessages = new HashSet<Guid>();

        private Socket socket;
        
        public EndPoint CurrentEndPoint { get; private set; }

        public NetworkClient(RealClient realClient)
        {
            this.realClient = realClient;
        }

        public void Start(EndPoint sourceEndPoint)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(sourceEndPoint);
            CurrentEndPoint = sourceEndPoint;
            socket.IOControl(
                (IOControlCode)(-1744830452),
                new byte[] { 0, 0, 0, 0 },
                null
            );
            socket.ReceiveBufferSize = realClient.Config.ReceiveBufferSize;
            socket.SendBufferSize = realClient.Config.SendBufferSize;
            var stateObject = new StateObject(realClient.Config.ReceiveBufferSize)
            {
                EndPoint = new IPEndPoint(IPAddress.Any, 0)
            };
            socket.BeginReceiveFrom(stateObject.Buffer, 0, stateObject.Buffer.Length, SocketFlags.None,
                ref stateObject.EndPoint, ReceiveCallback, stateObject);
            logger.Debug("NetworkClient started");
        }

        public void Stop()
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            CurrentEndPoint = null;
            logger.Debug("NetworkClient stopped");
        }

        private void ReceiveCallback(IAsyncResult asyncResult)
        {
            var currentStateObject = (StateObject) asyncResult.AsyncState;
            while (true)
            {
                try
                {
                    socket.EndReceiveFrom(asyncResult, ref currentStateObject.EndPoint);
                    break;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception e)
                {
                    logger.Error(e, "Exception in EndReceiveFrom");
                }
            }

            try
            {
                var message = new BaseMessage(currentStateObject.Buffer);
                if (!message.MessageType.ShouldBeEncrypted() 
                    || 
                    realClient.ClientsById.ContainsKey(message.MessageType.OnlyBroadcasted() ? message.RealSourceId : message.SourceId) 
                    && message.MessageType.ShouldBeEncrypted())
                {

                    if (message.MessageType.ShouldBeEncrypted())
                    {
                        var realSourceId = message.MessageFlags.HasFlag(MessageFlags.IsRedirected)
                            ? message.RealSourceId
                            : message.SourceId;
                        
                        var client = realClient.ClientsById[realSourceId];
                        var key = client.MainKey;
                        if (key == null)
                            throw new DNMPException("Selected client key is null");
                        message.Payload = SymmetricHelper.Decrypt(key, message.Payload);
                        message.ReceivedHash = message.Payload.Take(NetworkHashUtil.GetHashSize()).ToArray();
                        message.Payload = message.Payload.Skip(NetworkHashUtil.GetHashSize()).ToArray();
                        if (!NetworkHashUtil.ComputeChecksum(message.Payload).SequenceEqual(message.ReceivedHash))
                            throw new DNMPException("Hash of packets is not equal");
                    }

                    var ok = true;

                    lock (receivedReliableMessages)
                    {
                        if (message.MessageType.IsReliable() &&
                            !receivedReliableMessages.Contains(message.Guid))
                        {
                            receivedReliableMessages.Add(message.Guid);
                            SendBaseMessage(
                                new BaseMessage(new ReliableConfirmMessage(message.Guid),
                                    realClient.SelfClient?.Id ?? 0xFFFF,
                                    message.MessageFlags.HasFlag(MessageFlags.IsRedirected) ? message.RealSourceId : message.SourceId),
                                currentStateObject.EndPoint);
                        }
                        else if (message.MessageType.IsReliable())
                        {
                            ok = false;
                        }
                    }

                    if (ok)
                    {
                        realClient.MessageHandler.ProcessMessage(message, currentStateObject.EndPoint);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Exception on receivng message");
            }

            currentStateObject.EndPoint = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                try
                {
                    socket.BeginReceiveFrom(currentStateObject.Buffer, 0, currentStateObject.Buffer.Length,
                        SocketFlags.None,
                        ref currentStateObject.EndPoint, ReceiveCallback, currentStateObject);
                    break;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception e)
                {
                    logger.Error(e, "Exception in BeginReceiveFrom");
                }
            }
        }
        
        private void SendRawBytes(byte[] data, EndPoint endPoint)
        {
            socket.SendTo(data, endPoint);
        }

        public void SendBaseMessage(BaseMessage message, EndPoint endPoint)
        {
            if (message.MessageType.ShouldBeEncrypted())
            {
                var client = realClient.ClientsById[message.RealDestinationId];
                client.BytesSent += message.TotalLength;
                if (message.MessageType == MessageType.Data)
                    client.DataBytesSent += message.Payload.Length;
                var key = client.MainKey;
                if (key == null)
                {
                    realClient.MessageHandler.DisconnectClient(client.Id);
                    throw new DNMPException("Selected client key is null");
                }
                message.Payload = SymmetricHelper.Encrypt(key, NetworkHashUtil.ComputeChecksum(message.Payload).Concat(message.Payload).ToArray());
                
            }
            SendRawBytes(message.GetBytes(), endPoint);
        }

        public void Dispose()
        {
            socket.Dispose();
        }
    }
}

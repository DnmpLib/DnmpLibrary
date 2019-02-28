using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DNMPLibrary.Client;
using DNMPLibrary.Core;
using DNMPLibrary.Interaction.Protocol;
using DNMPLibrary.Network.Messages;
using DNMPLibrary.Network.Messages.Types;
using DNMPLibrary.Security.Cryptography.Symmetric;
using DNMPLibrary.Util;
using NLog;

namespace DNMPLibrary.Network
{
    internal class NetworkHandler
    {
        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        
        private readonly DNMPClient realClient;
        private readonly HashSet<Guid> receivedReliableMessages = new HashSet<Guid>();
        
        public IEndPoint CurrentEndPoint { get; private set; }

        public readonly Protocol UsedProtocol;

        public NetworkHandler(DNMPClient realClient, Protocol usedProtocol)
        {
            this.realClient = realClient;
            UsedProtocol = usedProtocol;
            usedProtocol.OnReceive += ReceiveCallback;
        }

        public void Start(IEndPoint sourceEndPoint)
        {
            CurrentEndPoint = sourceEndPoint;
            UsedProtocol.Start(sourceEndPoint);
            reliableMessages.Clear();
        }

        public void Stop()
        {
            CurrentEndPoint = null;
            UsedProtocol.Stop();
        }

        private void ReceiveCallback(byte[] data, IEndPoint source)
        {
            try
            {
                var message = new BaseMessage(data);
                if (message.MessageType.ShouldBeEncrypted() &&
                    (!realClient.ClientsById.ContainsKey(message.MessageType.OnlyBroadcasted()
                         ? message.RealSourceId
                         : message.SourceId) || !message.MessageType.ShouldBeEncrypted()))
                    return;

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
                    message.ReceivedHash = message.Payload.Take(HashUtil.GetHashSize()).ToArray();
                    message.Payload = message.Payload.Skip(HashUtil.GetHashSize()).ToArray();
                    if (!message.SecurityHash.SequenceEqual(message.ReceivedHash))
                        throw new DNMPException("Hash of packets is not equal");
                }

                lock (receivedReliableMessages)
                {
                    if (message.MessageType.IsReliable() &&
                        !receivedReliableMessages.Contains(message.Guid))
                    {
                        if (!message.Hash.SequenceEqual(message.RealHash))
                        {
                            return;
                        }

                        receivedReliableMessages.Add(message.Guid);
                        SendBaseMessage(
                            new BaseMessage(new ReliableConfirmMessage(message.Guid),
                                realClient.SelfClient?.Id ?? 0xFFFF,
                                message.MessageFlags.HasFlag(MessageFlags.IsRedirected)
                                    ? message.RealSourceId
                                    : message.SourceId),
                            source);
                    }
                    else if (message.MessageType.IsReliable())
                    {
                        return;
                    }
                }
                    
                if (message.MessageType == MessageType.ReliableConfirm)
                {
                    var decodedMessage = new ReliableConfirmMessage(message.Payload);
                    if (!reliableMessages.ContainsKey(decodedMessage.MessageId))
                        return;
                    EventQueue.RemoveEvent(decodedMessage.MessageId);
                    reliableMessages.TryRemove(decodedMessage.MessageId, out _);
                }
                else
                    realClient.MessageHandler.ProcessMessage(message, source);
            }
            catch (Exception e)
            {
                logger.Error(e, "Exception on receivng message");
            }
        }
        
        private void SendRawBytes(byte[] data, IEndPoint endPoint)
        {
            UsedProtocol.Send(data, endPoint);
        }

        public void SendBaseMessage(BaseMessage message, IEndPoint endPoint)
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
                message = new BaseMessage(
                    SymmetricHelper.Encrypt(key, message.SecurityHash.Concat(message.Payload).ToArray()),
                    message.MessageType, 
                    message.SourceId, message.DestinationId, 
                    message.RealSourceId, message.RealDestinationId, 
                    message.Guid
                );
            }
            SendRawBytes(message.GetBytes(), endPoint);
        }
        
        private readonly ConcurrentDictionary<Guid, BaseMessagePair> reliableMessages =
            new ConcurrentDictionary<Guid, BaseMessagePair>();

        private class BaseMessagePair
        {
            public IEndPoint EndPoint;
            public BaseMessage Message;
            public int Tries;
        }

        public void SendReliableMessage(ITypedMessage typedMessage, ushort sourceId, ushort destinationId, IEndPoint to)
        {
            SendReliableMessage(typedMessage, sourceId, destinationId, destinationId, to);
        }

        public void SendReliableMessage(ITypedMessage typedMessage, ushort sourceId, ushort destinationId)
        {
            SendReliableMessage(typedMessage, sourceId, destinationId,
                realClient.ClientsById[destinationId].RedirectPing.Id == 0xFFFF
                    ? realClient.ClientsById[destinationId].Id
                    : realClient.ClientsById[realClient.ClientsById[destinationId].RedirectPing.Id].Id,
                realClient.ClientsById[destinationId].RedirectPing.Id == 0xFFFF
                    ? realClient.ClientsById[destinationId].EndPoint
                    : realClient.ClientsById[realClient.ClientsById[destinationId].RedirectPing.Id].EndPoint);
        }

        public void BroadcastMessage(ITypedMessage typedMessage, ushort from, ushort except = 0xFFFF)
        {
            try
            {
                if (!typedMessage.GetMessageType().OnlyBroadcasted())
                    throw new DNMPException("Message is not broadcastable");
                foreach (var client in realClient.ClientsById.Values.Where(x =>
                    x.ParentId == realClient.SelfClient.Id || x.Id == realClient.SelfClient.ParentId))
                {
                    if (client.Id == except)
                        continue;
                    if (typedMessage.GetMessageType() == MessageType.DataBroadcast)
                        client.DataBytesSent += typedMessage.GetBytes().Length;
                    SendReliableMessage(typedMessage, from, 0xFFFF, client.Id, client.EndPoint);
                }
            }
            catch (Exception)
            {
                if (realClient.CurrentStatus == DNMPClient.ClientStatus.Disconnecting ||
                    realClient.CurrentStatus == DNMPClient.ClientStatus.NotConnected)
                    return;
                throw;
            }
        }

        public void SendReliableMessage(ITypedMessage typedMessage, ushort sourceId, ushort destinationId,
            ushort realDestinationId, IEndPoint to)
        {
            try
            {
                if (!typedMessage.GetMessageType().IsReliable())
                    throw new DNMPException("Message is not reliable");
                var id = Guid.NewGuid();
                var message = new BaseMessage(typedMessage, sourceId, destinationId, realClient.SelfClient.Id, realDestinationId, id);
                var messageInfo = new BaseMessagePair
                {
                    Message = message,
                    EndPoint = to
                };
                reliableMessages.TryAdd(id, messageInfo);
                realClient.NetworkHandler.SendBaseMessage(message, to);
                EventQueue.AddEvent(ReliableCallback, new KeyValuePair<Guid, BaseMessagePair>(id, messageInfo),
                    DateTime.Now.AddMilliseconds(500), id);
            }
            catch (Exception)
            {
                if (realClient.CurrentStatus == DNMPClient.ClientStatus.Disconnecting || realClient.CurrentStatus == DNMPClient.ClientStatus.NotConnected)
                    return;
                throw;
            }
        }

        private void ReliableCallback(object messageObject)
        {
            try
            {
                var currentMessageInfo = (KeyValuePair<Guid, BaseMessagePair>)messageObject;
                currentMessageInfo.Value.Tries++;
                if (currentMessageInfo.Value.Tries > realClient.Config.MaxReliableRetries ||
                    !realClient.ClientsById.ContainsKey(currentMessageInfo.Value.Message.RealDestinationId))
                {
                    reliableMessages.TryRemove(currentMessageInfo.Key, out var _);
                    return;
                }

                realClient.NetworkHandler.SendBaseMessage(currentMessageInfo.Value.Message, currentMessageInfo.Value.EndPoint);
                EventQueue.AddEvent(ReliableCallback,
                    new KeyValuePair<Guid, BaseMessagePair>(currentMessageInfo.Key, currentMessageInfo.Value),
                    DateTime.Now.AddMilliseconds(500), currentMessageInfo.Key);
            }
            catch (Exception e)
            {
                logger.Error(e, "Exception in ReliableCallback");
            }
        }

    }
}

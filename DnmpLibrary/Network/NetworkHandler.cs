using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DnmpLibrary.Client;
using DnmpLibrary.Core;
using DnmpLibrary.Interaction.Protocol;
using DnmpLibrary.Network.Messages;
using DnmpLibrary.Network.Messages.Types;
using DnmpLibrary.Security.Cryptography.Symmetric;
using DnmpLibrary.Util;
using NLog;

namespace DnmpLibrary.Network
{
    internal class NetworkHandler
    {
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly DnmpClient realClient;
        private readonly HashSet<Guid> receivedReliableMessages = new HashSet<Guid>();

        public IEndPoint CurrentEndPoint { get; private set; }

        public readonly Protocol UsedProtocol;

        public NetworkHandler(DnmpClient realClient, Protocol usedProtocol)
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
            receivedReliableMessages.Clear();
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
                        throw new DnmpException("Selected client key is null");
                    message.Payload = SymmetricHelper.Decrypt(key, message.Payload);
                    message.ReceivedHash = message.Payload.Take(HashUtil.GetHashSize()).ToArray();
                    message.Payload = message.Payload.Skip(HashUtil.GetHashSize()).ToArray();
                    if (!message.SecurityHash.SequenceEqual(message.ReceivedHash))
                        throw new DnmpException("Hash of packets is not equal");
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

                if (realClient.ClientsById.ContainsKey(message.SourceId) &&
                    realClient.ClientsById[message.SourceId].EndPoint.Equals(source))
                    realClient.ClientsById[message.SourceId].BytesReceived += message.TotalLength;
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
                if (message.MessageType == MessageType.Data && realClient.ClientsById.ContainsKey(message.DestinationId))
                    realClient.ClientsById[message.DestinationId].DataBytesSent += message.Payload.Length;
                var key = client.MainKey;
                if (key == null)
                {
                    realClient.MessageHandler.DisconnectClient(client.Id);
                    throw new DnmpException($"Selected client key is null; Client Id: [{client.Id}]");
                }
                message = new BaseMessage(
                    SymmetricHelper.Encrypt(key, message.SecurityHash.Concat(message.Payload).ToArray()),
                    message.MessageType,
                    message.SourceId, message.DestinationId,
                    message.RealSourceId, message.RealDestinationId,
                    message.Guid,
                    message.MessageFlags
                );
            }
            if (realClient.ClientsById.ContainsKey(message.RealDestinationId))
                realClient.ClientsById[message.RealDestinationId].BytesSent += message.TotalLength;
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

        public void SendReliableMessage(ITypedMessage typedMessage, ushort sourceId, ushort destinationId, IEndPoint to, MessageFlags messageFlags = MessageFlags.None)
        {
            SendReliableMessage(typedMessage, sourceId, destinationId, destinationId, to, messageFlags);
        }

        public void SendReliableMessage(ITypedMessage typedMessage, ushort sourceId, ushort destinationId, MessageFlags messageFlags = MessageFlags.None)
        {
            SendReliableMessage(typedMessage, sourceId, destinationId,
                realClient.ClientsById[destinationId].RedirectPing.Id == 0xFFFF
                    ? realClient.ClientsById[destinationId].Id
                    : realClient.ClientsById[realClient.ClientsById[destinationId].RedirectPing.Id].Id,
                realClient.ClientsById[destinationId].RedirectPing.Id == 0xFFFF
                    ? realClient.ClientsById[destinationId].EndPoint
                    : realClient.ClientsById[realClient.ClientsById[destinationId].RedirectPing.Id].EndPoint, messageFlags);
        }

        public void BroadcastMessage(ITypedMessage typedMessage, ushort from, ushort except = 0xFFFF)
        {
            try
            {
                if (!typedMessage.GetMessageType().OnlyBroadcasted())
                    throw new DnmpException("Message is not broadcastable");
                foreach (var client in realClient.ClientsById.Values.Where(x =>
                    x.ParentId == realClient.SelfClient.Id || x.Id == realClient.SelfClient.ParentId))
                {
                    if (client.Id == except)
                        continue;
                    if (typedMessage.GetMessageType() == MessageType.DataBroadcast)
                        client.DataBytesSent += typedMessage.GetBytes().Length;
                    SendReliableMessage(typedMessage, from, 0xFFFF, client.Id, client.EndPoint, MessageFlags.IsRedirected);
                }
            }
            catch (Exception)
            {
                if (realClient.CurrentStatus == DnmpClient.ClientStatus.Disconnecting ||
                    realClient.CurrentStatus == DnmpClient.ClientStatus.NotConnected)
                    return;
                throw;
            }
        }

        public void SendReliableMessage(ITypedMessage typedMessage, ushort sourceId, ushort destinationId,
            ushort realDestinationId, IEndPoint to, MessageFlags messageFlags = MessageFlags.None)
        {
            try
            {
                if (!typedMessage.GetMessageType().IsReliable())
                    throw new DnmpException("Message is not reliable");
                var id = Guid.NewGuid();
                var message = new BaseMessage(typedMessage, sourceId, destinationId, realClient.SelfClient?.Id ?? 0xFFFF, realDestinationId, id, messageFlags);
                var messageInfo = new BaseMessagePair
                {
                    Message = message,
                    EndPoint = to
                };
                reliableMessages.TryAdd(id, messageInfo);
                SendBaseMessage(message, to);
                EventQueue.AddEvent(ReliableCallback, new KeyValuePair<Guid, BaseMessagePair>(id, messageInfo),
                    DateTime.Now.AddMilliseconds(500), id);
            }
            catch (Exception)
            {
                if (realClient.CurrentStatus == DnmpClient.ClientStatus.Disconnecting || realClient.CurrentStatus == DnmpClient.ClientStatus.NotConnected)
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

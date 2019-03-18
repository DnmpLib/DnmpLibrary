using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using DnmpLibrary.Client;
using DnmpLibrary.Core;
using DnmpLibrary.Interaction.MessageInterface;
using DnmpLibrary.Interaction.Protocol;
using DnmpLibrary.Network.Messages;
using DnmpLibrary.Network.Messages.Types;
using DnmpLibrary.Security.Cryptography.Asymmetric;
using DnmpLibrary.Security.Cryptography.Symmetric;
using DnmpLibrary.Util;
using NLog;

namespace DnmpLibrary.Handlers
{
    internal class MessageHandler : IDisposable
    {
        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        
        private readonly Random random = new Random();
        private readonly DnmpClient realClient;

        private readonly RandomNumberGenerator secureRandom = RandomNumberGenerator.Create();

        private readonly ConcurrentDictionary<IEndPoint, byte[]> tokens = new ConcurrentDictionary<IEndPoint, byte[]>();

        private ISymmetricKey tempConnectionSymmetricKey;

        private ushort[] fixingTo;
        private Guid reconnectionTimeoutExceededEventGuid;
        private Guid rebalanceGraphEventGuid;

        private Timer heartbeatTimer;
        private Timer pingUpdateTimer;
        private Timer directClientPingTimer;

        public MessageHandler(DnmpClient realClient)
        {
            this.realClient = realClient;

            realClient.OnConnected += () => rebalanceGraphEventGuid = EventQueue.AddEvent(RebalanceGraph, null, DateTime.Now.AddSeconds(10));
            realClient.OnDisconnected += () => EventQueue.RemoveEvent(rebalanceGraphEventGuid);
            realClient.OnClientParentChange += (clientId, newParentId, oldParentId) =>
            {
                if (oldParentId == 0xFFFF)
                    return;
                if (clientId == realClient.SelfClient.Id)
                    EventQueue.RemoveEvent(realClient.ClientsById[oldParentId].DisconnectEventGuid);
                if (oldParentId == realClient.SelfClient.Id)
                    EventQueue.RemoveEvent(realClient.ClientsById[clientId].DisconnectEventGuid);
            };
        }

        public async void ProcessMessage(BaseMessage message, IEndPoint from)
        {
            try
            {
                if (message.MessageType.OnlyBroadcasted())
                {
                    realClient.NetworkHandler.BroadcastMessage(new DummyMessage(message), message.SourceId, message.RealSourceId);
                    if (message.SourceId == realClient.SelfClient.Id)
                        return;
                }
                else if (message.DestinationId != 0xFFFF && message.MessageType.ShouldBeEncrypted() && message.DestinationId != realClient.SelfClient.Id)
                {
                    if (!message.MessageFlags.HasFlag(MessageFlags.IsRedirected))
                        return;
                    var redirectId = realClient.ClientsById[message.DestinationId].RedirectPing.Id;
                    if (message.MessageType.IsReliable())
                        realClient.NetworkHandler.SendReliableMessage(new DummyMessage(message), message.SourceId, message.DestinationId);
                    else
                    {
                        message.RealDestinationId = redirectId == 0xFFFF ? message.DestinationId : redirectId;
                        message.RealSourceId = realClient.SelfClient.Id;
                        realClient.NetworkHandler.SendBaseMessage(message, redirectId == 0xFFFF
                            ? realClient.ClientsById[message.DestinationId].EndPoint
                            : realClient.ClientsById[redirectId].EndPoint);
                    }

                    return;
                }

                var realSourceId = message.MessageFlags.HasFlag(MessageFlags.IsRedirected) ? message.RealSourceId : message.SourceId;

                if (realClient.ClientsById.ContainsKey(realSourceId) && message.MessageType.ShouldBeEncrypted())
                {
                    realClient.ClientsById[realSourceId].EndPoint = from;
                    realClient.ClientsById[realSourceId].BytesReceived += message.TotalLength;
                }

                switch (message.MessageType)
                {
                    case MessageType.ConnectionRequest:
                        {
                            if (realClient.CurrentStatus != DnmpClient.ClientStatus.Connected)
                                return;
                            if (message.SourceId != 0xFFFF || message.DestinationId != 0xFFFF)
                                return;
                            if (realClient.ClientsById.Any(x => x.Value.EndPoint.Equals(from)) || realClient.SelfClient.EndPoint.Equals(from))
                                return;

                            var decodedMessage = new ConnectionRequestMessage(message.Payload);

                            if (!decodedMessage.NetworkId.SequenceEqual(realClient.Key.GetNetworkId()))
                                return;

                            var token = new byte[realClient.Config.TokenSize];
                            secureRandom.GetBytes(token);
                            
                            tokens.TryRemove(from, out _);
                            tokens.TryAdd(from, token);

                            realClient.NetworkHandler.SendBaseMessage(
                                new BaseMessage(new ConnectionRequestReplyMessage
                                {
                                    Token = token
                                }, 0xFFFF, 0xFFFF),
                                from);
                        }
                        break;
                    case MessageType.ConnectionRequestReply:
                        {
                            if (realClient.CurrentStatus != DnmpClient.ClientStatus.Connecting)
                                return;
                            if (message.SourceId != 0xFFFF || message.DestinationId != 0xFFFF)
                                return;

                            var requestReplyMessage = new ConnectionRequestReplyMessage(message.Payload);
                            if (requestReplyMessage.Token.Length == 0)
                                return;

                            realClient.CurrentStatus = DnmpClient.ClientStatus.Handshaking;

                            tempConnectionSymmetricKey = realClient.DummySymmetricKey.GenerateNewKey();

                            realClient.NetworkHandler.SendBaseMessage(
                                new BaseMessage(new ConnectionRequestConfirmMessage
                                {
                                    EncryptedToken = AsymmetricHelper.Sign(realClient.Key, requestReplyMessage.Token),
                                    EncryptedKey = AsymmetricHelper.Encrypt(realClient.Key, tempConnectionSymmetricKey.GetBytes()),
                                    EncryptedClientData = SymmetricHelper.Encrypt(tempConnectionSymmetricKey, realClient.SelfCustomData)
                                },
                                    0xFFFF, 0xFFFF),
                                from);
                        }
                        break;
                    case MessageType.ConnectionRequestConfirm:
                        {
                            if (realClient.CurrentStatus != DnmpClient.ClientStatus.Connected)
                                return;
                            if (message.SourceId != 0xFFFF || message.DestinationId != 0xFFFF)
                                return;
                            if (!tokens.ContainsKey(from))
                                return;

                            var requestConfirmMessage = new ConnectionRequestConfirmMessage(message.Payload);

                            tokens.TryRemove(from, out var token);

                            if (!AsymmetricHelper.Verify(realClient.Key, token, requestConfirmMessage.EncryptedToken))
                                return;

                            var newId = (ushort)Enumerable.Range(0, realClient.MessageInterface.GetMaxClientCount())
                                .Except(realClient.ClientsById.Values.Select(x => (int)x.Id))
                                .Except(new int[] { realClient.SelfClient.Id }).First();

                            var mainKey = realClient.DummySymmetricKey.CreateFromBytes(
                                AsymmetricHelper.Decrypt(realClient.Key, requestConfirmMessage.EncryptedKey));

                            var newClient = new DnmpNode
                            {
                                Id = newId,
                                EndPoint = from,
                                MainKey = mainKey,
                                Flags = ClientFlags.SymmetricKeyExchangeDone | ClientFlags.DirectConnectionAvailable,
                                CustomData = SymmetricHelper.Decrypt(mainKey, requestConfirmMessage.EncryptedClientData)
                            };

                            realClient.NetworkHandler.BroadcastMessage(new ConnectionNotificationMessage(newClient, realClient.NetworkHandler.UsedProtocol.GetEndPointFactory()),
                                realClient.SelfClient.Id);

                            realClient.AddClient(newClient);

                            realClient.ClientsById[newId].DisconnectEventGuid = EventQueue.AddEvent(
                                DisconnectClient, newId,
                                DateTime.Now.AddMilliseconds(realClient.Config.ClientTimeout));

                            ChangeClientParent(newId, realClient.SelfClient.Id);

                            realClient.NetworkHandler.SendReliableMessage(new ConnectionRequestConfirmReplyMessage(
                                    realClient.ClientsById.Values.Concat(new List<DnmpNode> { realClient.SelfClient })
                                        .ToList(), newId, from, realClient.NetworkHandler.UsedProtocol.GetEndPointFactory()),
                                realClient.SelfClient.Id, newId);
                        }
                        break;
                    case MessageType.ConnectionRequestConfirmReply:
                        {
                            if (realClient.CurrentStatus != DnmpClient.ClientStatus.Handshaking)
                                return;

                            var decodedMessage = new ConnectionRequestConfirmReplyMessage(message.Payload, realClient.NetworkHandler.UsedProtocol.GetEndPointFactory());

                            if (decodedMessage.Clients.Count == 0)
                                return;

                            realClient.SelfClient = new DnmpNode
                            {
                                Id = decodedMessage.NewId,
                                EndPoint = decodedMessage.NewEndPoint,
                                CustomData = realClient.SelfCustomData
                            };

                            ChangeClientParent(decodedMessage.NewId, message.SourceId);

                            realClient.MessageInterface.Initialize(realClient.SelfClient.Id);

                            realClient.ClientsById.Clear();
                            foreach (var client in decodedMessage.Clients)
                                realClient.AddClient(client);

                            realClient.ClientsById[message.SourceId].MainKey = tempConnectionSymmetricKey;
                            realClient.ClientsById[message.SourceId].Flags |= ClientFlags.DirectConnectionAvailable;
                            realClient.ClientsById[message.SourceId].Flags |= ClientFlags.SymmetricKeyExchangeDone;
                            realClient.ClientsById[message.SourceId].EndPoint = from;

                            realClient.NetworkHandler.BroadcastMessage(new ParentSwitchMessage(message.SourceId), decodedMessage.NewId);

                            realClient.ClientsById[message.SourceId].DisconnectEventGuid = EventQueue.AddEvent(
                                DisconnectClient, message.SourceId,
                                DateTime.Now.AddMilliseconds(realClient.Config.ClientTimeout));

                            var tempBuffer = new byte[realClient.Config.PingSize];
                            random.NextBytes(tempBuffer);

                            foreach (var client in realClient.ClientsById.Values)
                            {
                                if (client.Id == message.SourceId)
                                    continue;
                                client.LastPingSendTime = DateTime.Now;
                                realClient.NetworkHandler.SendBaseMessage(new BaseMessage(new PingMessage(tempBuffer),
                                    realClient.SelfClient.Id, client.Id), client.EndPoint);
                            }

                            pingUpdateTimer = new Timer(PingUpdateTimerCallback, null, realClient.Config.PingUpdateTimerStartDelay, realClient.Config.PingUpdateTimerDelay);

                            realClient.SelfConnected();
                        }
                        break;
                    case MessageType.ConnectionNotification: //TODO
                        {
                            var decodedMessage = new ConnectionNotificationMessage(message.Payload, realClient.NetworkHandler.UsedProtocol.GetEndPointFactory());
                            if (decodedMessage.Client.Id == realClient.SelfClient.Id)
                                break;
                            realClient.AddClient(decodedMessage.Client);
                        }
                        break;
                    case MessageType.DisconnectionNotification:
                        {
                            var decodedMessage = new DisconnectionNotificationMessage(message.Payload);
                            if (realClient.ClientsById.ContainsKey(decodedMessage.Id))
                                realClient.RemoveClient(realClient.ClientsById[decodedMessage.Id]);
                        }
                        break;
                    case MessageType.PingUpdate:
                        {
                            var decodedMessage = new PingUpdateMessage(message.Payload);

                            var pingPairs = decodedMessage.UnDirectClientIds.Where(x => x != realClient.SelfClient.Id)
                                .Select(client => new PingPair
                                {
                                    Id = client,
                                    Ping = realClient.ClientsById.ContainsKey(client)
                                        ? realClient.ClientsById[client].DirectPing
                                        : (ushort) 0xFFFF
                                }).ToList();

                            realClient.NetworkHandler.SendReliableMessage(new PingUpdateReplyMessage(pingPairs.ToArray()), realClient.SelfClient.Id,
                                message.SourceId);
                        }
                        break;
                    case MessageType.PingUpdateReply:
                        {
                            var decodedMessage = new PingUpdateReplyMessage(message.Payload);
                            foreach (var pingPair in decodedMessage.UnDirectClientPings)
                            {
                                var client = realClient.ClientsById[pingPair.Id];
                                var sourceClient = realClient.ClientsById[message.SourceId];
                                var previousPing = client.RedirectPing.Ping;
                                if (client.RedirectPing.Ping <= pingPair.Ping + sourceClient.DirectPing)
                                    continue;
                                client.RedirectPing = new PingPair
                                {
                                    Ping = (ushort) (pingPair.Ping + sourceClient.DirectPing),
                                    Id = message.SourceId
                                };
                                if (previousPing != 0xFFFF || client.Flags.HasFlag(ClientFlags.SymmetricKeyExchangeDone))
                                    continue;
                                while (client.DataMessageQueue.Any())
                                {
                                    client.DataMessageQueue.TryDequeue(out var dataBytes);
                                    realClient.NetworkHandler.SendBaseMessage(
                                        new BaseMessage(new DataMessage(dataBytes, false), realClient.SelfClient.Id, client.Id),
                                        realClient.ClientsById[client.RedirectPing.Id].EndPoint);
                                }
                            }
                        }
                        break;
                    case MessageType.Heartbeat:
                        {
                            EventQueue.RemoveEvent(realClient.ClientsById[message.SourceId].DisconnectEventGuid);
                            realClient.ClientsById[message.SourceId].DisconnectEventGuid = EventQueue.AddEvent(
                                DisconnectClient, message.SourceId,
                                DateTime.Now.AddMilliseconds(realClient.Config.ClientTimeout));
                        }
                        break;
                    case MessageType.ParentSwitch:
                        {
                            var decodedMessage = new ParentSwitchMessage(message.Payload);
                            var previousParentId = realClient.ClientsById[message.SourceId].ParentId;
                            ChangeClientParent(message.SourceId, decodedMessage.ParentId);

                            if (decodedMessage.ParentId == realClient.SelfClient.Id || (previousParentId == realClient.SelfClient.Id && previousParentId != decodedMessage.ParentId))
                            {
                                EventQueue.RemoveEvent(realClient.ClientsById[message.SourceId].DisconnectEventGuid);
                                realClient.ClientsById[message.SourceId].DisconnectEventGuid = EventQueue.AddEvent(
                                    DisconnectClient, message.SourceId,
                                    DateTime.Now.AddMilliseconds(realClient.Config.ClientTimeout));
                            }
                        }
                        break;
                    case MessageType.Ping:
                        {
                            realClient.NetworkHandler.SendBaseMessage(
                                new BaseMessage(new PongMessage(message.Payload), realClient.SelfClient.Id,
                                    message.SourceId),
                                from);
                        }
                        break;
                    case MessageType.Pong:
                        {
                            if (realClient.ClientsById[message.SourceId].LastPingSendTime == DateTime.MinValue ||
                                realClient.ClientsById[message.SourceId].RedirectPing.Id != 0xFFFF)
                                break;
                            realClient.ClientsById[message.SourceId].DirectPing =
                                (ushort)(DateTime.Now - realClient.ClientsById[message.SourceId].LastPingSendTime)
                                .TotalMilliseconds;
                            if (!realClient.ClientsById[message.SourceId].Flags.HasFlag(ClientFlags.DirectConnectionAvailable))
                            {
                                var symmetricKey = realClient.DummySymmetricKey.GenerateNewKey();
                                realClient.ClientsById[message.SourceId].MainKey = symmetricKey;
                                realClient.ClientsById[message.SourceId].Flags |= ClientFlags.SymmetricKeyExchangeInProgress;
                                realClient.NetworkHandler.SendReliableMessage(new SecondRankConnectionRequestMessage(symmetricKey.GetBytes(), realClient.Key, false),
                                    realClient.SelfClient.Id, message.SourceId,
                                    realClient.ClientsById[message.SourceId].EndPoint);
                            }

                            realClient.ClientsById[message.SourceId].Flags |= ClientFlags.DirectConnectionAvailable;
                            realClient.ClientsById[message.SourceId].LastPingSendTime = DateTime.MinValue;
                            realClient.ClientsById[message.SourceId].LastPingReceiveTime = DateTime.Now;
                        }
                        break;
                    case MessageType.SecondRankConnectionRequest:
                        {
                            var secondRankConnectionRequestMessage =
                                new SecondRankConnectionRequestMessage(message.Payload, realClient.Key);
                            var client = realClient.ClientsById[message.SourceId];
                            
                            client.MainKey = realClient.DummySymmetricKey.CreateFromBytes(secondRankConnectionRequestMessage.SymmetricKeyBytes);
                            client.Flags |= ClientFlags.SymmetricKeyExchangeDone;
                            client.Flags |= ClientFlags.DirectConnectionAvailable;

                            realClient.SecondaryConnectionStart(message.SourceId);
                            realClient.NetworkHandler.SendReliableMessage(new SecondRankConnectionResponseMessage(), realClient.SelfClient.Id,
                                message.SourceId, from);
                        }
                        break;
                    case MessageType.SecondRankConnectionResponse:
                        {
                            var client = realClient.ClientsById[message.SourceId];

                            realClient.SecondaryConnectionStart(message.SourceId);

                            client.EndPoint = from;
                            client.Flags |= ClientFlags.SymmetricKeyExchangeDone;
                            client.Flags &= ~ClientFlags.SymmetricKeyExchangeInProgress;
                            while (client.DataMessageQueue.Any())
                            {
                                client.DataMessageQueue.TryDequeue(out var dataBytes);
                                realClient.NetworkHandler.SendBaseMessage(
                                    new BaseMessage(new DataMessage(dataBytes, false), realClient.SelfClient.Id, client.Id),
                                    client.EndPoint);
                            }
                        }
                        break;
                    case MessageType.ReliableConfirm:
                        throw new DnmpException("ReliableConfirm in MessageHandler");
                    case MessageType.TryRestablishConnection:
                        {
                            var decodedMessage = new TryRestablishConnectionMessage(message.Payload);
                            fixingTo = decodedMessage.ClientIds;
                            foreach (var i in fixingTo)
                            {
                                if (!realClient.ClientsById[i].Flags.HasFlag(ClientFlags.SymmetricKeyExchangeDone))
                                    continue;
                                realClient.NetworkHandler.SendReliableMessage(
                                    new SubnetworkSpanningUpdateMessage(GetSelfComponent().Select(x =>
                                        new KeyValuePair<ushort, ushort>(x, realClient.ClientsById[x].ParentId)).ToArray()),
                                    realClient.SelfClient.Id, i);
                                ChangeClientParent(realClient.SelfClient.Id, message.SourceId);
                                realClient.NetworkHandler.BroadcastMessage(new ParentSwitchMessage(message.SourceId), realClient.SelfClient.Id);
                                realClient.NetworkHandler.SendReliableMessage(new TryRestablishConnectionReplyMessage(true), realClient.SelfClient.Id,
                                    realClient.SelfClient.ParentId);

                                return;
                            }

                            if (realClient.ClientsById.Values.Any(x => x.ParentId == realClient.SelfClient.Id))
                                realClient.NetworkHandler.SendReliableMessage(new TryRestablishConnectionMessage(fixingTo), realClient.SelfClient.Id,
                                    realClient.ClientsById.Values.Where(x => x.ParentId == realClient.SelfClient.Id)
                                        .OrderBy(x => x.Id).First().Id);
                        }
                        break;
                    case MessageType.TryRestablishConnectionReply:
                        {
                            var decodedMessage = new TryRestablishConnectionReplyMessage(message.Payload);
                            if (decodedMessage.Status)
                            {
                                ChangeClientParent(realClient.SelfClient.Id, message.SourceId);
                                realClient.NetworkHandler.BroadcastMessage(new ParentSwitchMessage(message.SourceId), realClient.SelfClient.ParentId);
                                realClient.NetworkHandler.SendReliableMessage(new TryRestablishConnectionReplyMessage(true), realClient.SelfClient.Id,
                                    realClient.SelfClient.ParentId);
                                return;
                            }

                            var next = realClient.ClientsById.Values.Where(x => x.ParentId == realClient.SelfClient.Id)
                                .OrderBy(x => x.Id).SkipWhile(x => x.Id != message.SourceId).Skip(1).FirstOrDefault();
                            if (next == default(DnmpNode))
                                realClient.NetworkHandler.SendReliableMessage(new TryRestablishConnectionReplyMessage(false),
                                    realClient.SelfClient.Id, realClient.SelfClient.ParentId);
                            else
                                realClient.NetworkHandler.SendReliableMessage(new TryRestablishConnectionMessage(fixingTo), realClient.SelfClient.Id,
                                    next.Id);
                        }
                        break;
                    case MessageType.Data:
                        {
                            var receivedMessage = new DataMessage(message.Payload);

                            realClient.ClientsById[message.SourceId].DataBytesReceived += receivedMessage.Payload.Length;

                            realClient.MessageInterface.PacketReceived(this, new MessageInterface.DataMessageEventArgs
                            {
                                Data = new DataMessage(message.Payload).Payload,
                                SourceId = message.SourceId
                            });
                        }
                        break;
                    case MessageType.DataBroadcast:
                        {
                            var receivedMessage = new DataBroadcastMessage(message.Payload);

                            realClient.ClientsById[message.SourceId].DataBytesReceived += receivedMessage.Payload.Length;

                            realClient.MessageInterface.PacketReceived(this, new MessageInterface.DataMessageEventArgs
                            {
                                Data = receivedMessage.Payload,
                                SourceId = message.DestinationId,
                                IsBroadcast = true
                            });
                        }
                        break;
                    case MessageType.SubnetworkSpanningUpdate:
                        {
                            var decodedMessage = new SubnetworkSpanningUpdateMessage(message.Payload);
                            realClient.NetworkHandler.BroadcastMessage(new BroadcastedSubnetworkSpanningUpdateMessage(decodedMessage.Clients),
                                realClient.SelfClient.Id, message.SourceId);
                            foreach (var client in decodedMessage.Clients)
                            {
                                ChangeClientParent(client.Key == realClient.SelfClient.Id ? realClient.SelfClient.Id : client.Key,
                                    client.Value);
                            }

                            realClient.NetworkHandler.SendReliableMessage(new SubnetworkSpanningUpdateReplyMessage(GetSelfComponent(message.SourceId).Where(x => x == realClient.SelfClient.Id || realClient.ClientsById.ContainsKey(x))
                                    .Select(x =>
                                        new KeyValuePair<ushort, ushort>(x, x == realClient.SelfClient.Id ? realClient.SelfClient.ParentId : realClient.ClientsById[x].ParentId)).ToArray()),
                                realClient.SelfClient.Id, message.SourceId);
                        }
                        break;
                    case MessageType.BroadcastedSubnetworkSpanningUpdate:
                        {
                            var decodedMessage = new SubnetworkSpanningUpdateMessage(message.Payload);
                            foreach (var client in decodedMessage.Clients)
                            {
                                ChangeClientParent(client.Key == realClient.SelfClient.Id ? realClient.SelfClient.Id : client.Key,
                                    client.Value);
                            }
                        }
                        break;
                    case MessageType.SubnetworkSpanningUpdateReply:
                        {
                            var decodedMessage = new SubnetworkSpanningUpdateReplyMessage(message.Payload);
                            realClient.NetworkHandler.BroadcastMessage(new BroadcastedSubnetworkSpanningUpdateMessage(decodedMessage.Clients),
                                realClient.SelfClient.Id, message.SourceId);
                            foreach (var client in decodedMessage.Clients)
                            {
                                ChangeClientParent(client.Key == realClient.SelfClient.Id ? realClient.SelfClient.Id : client.Key,
                                    client.Value);
                            }
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                await Task.Delay(0);
            }
            catch (Exception e)
            {
                if (realClient.CurrentStatus == DnmpClient.ClientStatus.Disconnecting ||
                    realClient.CurrentStatus == DnmpClient.ClientStatus.NotConnected)
                    return;
                logger.Error(e, $"Error on handling message {message.MessageType} from {from}: {e}");
            }
        }

        private void ChangeClientParent(ushort clientId, ushort newParentId)
        {
            var client = clientId == realClient.SelfClient.Id ? realClient.SelfClient : (realClient.ClientsById.ContainsKey(clientId) ? realClient.ClientsById[clientId] : null);
            if (client == null)
                return;
            var oldParentId = client.ParentId;
            if (newParentId == oldParentId)
                return;
            client.ParentId = newParentId;
            realClient.ClientParentChanged(clientId, newParentId, oldParentId);
        }

        private IEnumerable<ushort> GetSelfComponent(ushort except = 0xFFFF)
        {
            var graph = new Dictionary<ushort, List<ushort>>();
            foreach (var client in realClient.ClientsById.Values)
            {
                if (!graph.ContainsKey(client.ParentId))
                    graph.Add(client.ParentId, new List<ushort>());
                graph[client.ParentId].Add(client.Id);
            }

            var component = new HashSet<ushort>();
            var vertexQueue = new Queue<ushort>();

            vertexQueue.Enqueue(realClient.SelfClient.Id);
            while (vertexQueue.Any())
            {
                var vertex = vertexQueue.Dequeue();
                if (vertex == except)
                    continue;
                component.Add(vertex);
                if (!graph.ContainsKey(vertex))
                    continue;
                foreach (var nextVertex in graph[vertex])
                    vertexQueue.Enqueue(nextVertex);
            }

            return component;
        }

        public async Task<bool> SendMessage(byte[] data, ushort clientId)
        {
            if (realClient.CurrentStatus != DnmpClient.ClientStatus.Connected)
                return false;

            await Task.Delay(0);

            if (realClient.SelfClient.Id == clientId)
            {
                realClient.MessageInterface.PacketReceived(this, new MessageInterface.DataMessageEventArgs
                {
                    Data = data,
                    SourceId = clientId
                });
                return true;
            }

            var clientTo = realClient.ClientsById[clientId];
            if (clientTo.Flags.HasFlag(ClientFlags.SymmetricKeyExchangeDone))
            {
                realClient.NetworkHandler.SendBaseMessage(
                    new BaseMessage(new DataMessage(data, false), realClient.SelfClient.Id, clientTo.Id),
                    clientTo.EndPoint);
                return true;
            }

            if (clientTo.RedirectPing.Id != 0xFFFF && realClient.ClientsById.ContainsKey(clientTo.RedirectPing.Id))
            {
                realClient.NetworkHandler.SendBaseMessage(
                    new BaseMessage(new DataMessage(data, false), realClient.SelfClient.Id, clientTo.Id, realClient.SelfClient.Id, clientTo.RedirectPing.Id),
                    realClient.ClientsById[clientTo.RedirectPing.Id].EndPoint);
                return true;
            }

            if (clientTo.Flags.HasFlag(ClientFlags.SymmetricKeyExchangeInProgress))
            {
                clientTo.DataMessageQueue.Enqueue(data);
                return false;
            }

            if (clientTo.Flags.HasFlag(ClientFlags.DirectConnectionAvailable))
            {
                clientTo.DataMessageQueue.Enqueue(data);
                var symmetricKey = realClient.DummySymmetricKey.GenerateNewKey();
                realClient.ClientsById[clientTo.Id].MainKey = symmetricKey;
                clientTo.Flags |= ClientFlags.SymmetricKeyExchangeInProgress;
                realClient.NetworkHandler.SendReliableMessage(new SecondRankConnectionRequestMessage(symmetricKey.GetBytes(), realClient.Key, false),
                    realClient.SelfClient.Id, clientTo.Id,
                    clientTo.EndPoint);
                return false;
            }

            //Redirect is 0xFFFF

            if ((DateTime.Now - clientTo.LastForcePingUpdateTime).TotalMilliseconds > realClient.Config.ForcePingUpdateDelay)
            {
                var pingUpdateMessage = new PingUpdateMessage(new []{ clientTo.Id });
                foreach (var client in realClient.ClientsById.Values)
                    if (client.Flags.HasFlag(ClientFlags.DirectConnectionAvailable) &&
                        client.Flags.HasFlag(ClientFlags.SymmetricKeyExchangeDone))
                        realClient.NetworkHandler.SendReliableMessage(pingUpdateMessage, realClient.SelfClient.Id, client.Id);
                clientTo.LastForcePingUpdateTime = DateTime.Now;
            }

            clientTo.DataMessageQueue.Enqueue(data);
            return false;
        }

        public async Task BroadcastMessage(byte[] data)
        {
            if (realClient.CurrentStatus != DnmpClient.ClientStatus.Connected)
                return;
            await Task.Delay(0);
            realClient.NetworkHandler.BroadcastMessage(new DataBroadcastMessage(data, false), realClient.SelfClient.Id,
                realClient.SelfClient.Id);
        }

        internal void DisconnectClient(object idObject)
        {
            try
            {
                var disconnectedId = (ushort) idObject;

                if (!realClient.ClientsById.ContainsKey(disconnectedId))
                    return;
                if (disconnectedId != realClient.SelfClient.ParentId & realClient.ClientsById[disconnectedId].ParentId != realClient.SelfClient.Id)
                    return;

                if (disconnectedId == realClient.SelfClient.ParentId)
                {
                    ChangeClientParent(realClient.SelfClient.Id, 0xFFFF);
                    realClient.NetworkHandler.BroadcastMessage(new ParentSwitchMessage(0xFFFF), realClient.SelfClient.Id);
                }

                var disconnectedParentId = realClient.ClientsById[disconnectedId].ParentId;

                realClient.RemoveClient(realClient.ClientsById[disconnectedId]);
                realClient.NetworkHandler.BroadcastMessage(new DisconnectionNotificationMessage(disconnectedId), realClient.SelfClient.Id);

                EventQueue.RemoveEvent(reconnectionTimeoutExceededEventGuid);
                reconnectionTimeoutExceededEventGuid = EventQueue.AddEvent(ReconnectionTimeoutExceededCallback, null,
                    DateTime.Now.AddMilliseconds(realClient.Config.ReconnectionTimeout));

                var children = realClient.ClientsById.Values.Where(client => client.ParentId == disconnectedId)
                    .Concat(new List<DnmpNode> {realClient.SelfClient}).ToList();
                if (disconnectedId != realClient.SelfClient.ParentId)
                    return;

                children = children.OrderBy(x => x.Id).ToList();
                var graph = new Dictionary<ushort, List<ushort>>();
                foreach (var client in realClient.ClientsById.Values.Concat(
                    new List<DnmpNode> {realClient.SelfClient}))
                {
                    if (!graph.ContainsKey(client.Id))
                        graph.Add(client.Id, new List<ushort>());
                    if (client.ParentId == 0xFFFF)
                        continue;
                    if (!graph.ContainsKey(client.ParentId))
                        graph.Add(client.ParentId, new List<ushort>());
                    graph[client.Id].Add(client.ParentId);
                    graph[client.ParentId].Add(client.Id);
                }

                var used = new HashSet<ushort> {disconnectedId};

                var components = new Dictionary<ushort, HashSet<ushort>>();

                foreach (var child in children)
                {
                    if (used.Contains(child.Id))
                        continue;

                    components.Add(child.Id, new HashSet<ushort> {child.Id});
                    var queue = new Queue<ushort>();
                    queue.Enqueue(child.Id);
                    used.Add(child.Id);

                    while (queue.Count > 0)
                    {
                        var currentId = queue.Dequeue();
                        foreach (var clientId in graph[currentId])
                        {
                            if (used.Contains(clientId))
                                continue;
                            used.Add(clientId);
                            queue.Enqueue(clientId);
                            components[child.Id].Add(clientId);
                        }
                    }
                }

                if (disconnectedParentId != 0xFFFF)
                {
                    var queue = new Queue<ushort>();
                    queue.Enqueue(disconnectedParentId);
                    if (!components.ContainsKey(disconnectedParentId))
                        components.Add(disconnectedParentId, new HashSet<ushort> {disconnectedParentId});
                    while (queue.Count > 0)
                    {
                        var currentId = queue.Dequeue();
                        foreach (var clientId in graph[currentId])
                        {
                            if (used.Contains(clientId))
                                continue;
                            used.Add(clientId);
                            queue.Enqueue(clientId);
                            components[disconnectedParentId].Add(clientId);
                        }
                    }
                }

                var clients = new List<ushort>();

                foreach (var component in components)
                {
                    if (component.Key >= realClient.SelfClient.Id)
                        continue;
                    foreach (var clientId in component.Value)
                    {
                        if (clientId == disconnectedId)
                            continue;

                        if (!realClient.ClientsById[clientId].Flags.HasFlag(ClientFlags.SymmetricKeyExchangeDone))
                        {
                            clients.Add(clientId);
                            continue;
                        }
                        
                        ChangeClientParent(realClient.SelfClient.Id, clientId);
                        realClient.NetworkHandler.BroadcastMessage(new ParentSwitchMessage(clientId), realClient.SelfClient.Id);
                        realClient.NetworkHandler.SendReliableMessage(new SubnetworkSpanningUpdateMessage(components[realClient.SelfClient.Id]
                                .Select(x => new KeyValuePair<ushort, ushort>(x,
                                    x == realClient.SelfClient.Id
                                        ? realClient.SelfClient.ParentId
                                        : realClient.ClientsById[x].ParentId))
                                .ToArray()),
                            realClient.SelfClient.Id, clientId);
                        return;
                    }
                }

                fixingTo = clients.ToArray();
                foreach (var client in realClient.ClientsById.Values.OrderBy(x => x.Id))
                    if (client.ParentId == realClient.SelfClient.Id)
                    {
                        realClient.NetworkHandler.SendReliableMessage(new TryRestablishConnectionMessage(fixingTo),
                            realClient.SelfClient.Id, client.Id);
                        return;
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

        private void ReconnectionTimeoutExceededCallback(object state)
        {
            try
            {
                var allClients = realClient.ClientsById.Values.Union(new[] {realClient.SelfClient});
                var dynNetClients = allClients as DnmpNode[] ?? allClients.ToArray();
                var clientsDsu = new Dsu<DnmpNode>(dynNetClients);

                foreach (var client in dynNetClients)
                    if (realClient.ClientsById.ContainsKey(client.ParentId) ||
                        client.ParentId == realClient.SelfClient.Id)
                        clientsDsu.MergeSets(client,
                            client.ParentId == realClient.SelfClient.Id
                                ? realClient.SelfClient
                                : realClient.ClientsById[client.ParentId]);

                foreach (var client in dynNetClients)
                {
                    if (clientsDsu.InOneSet(realClient.SelfClient, client))
                        continue;
                    realClient.RemoveClient(client);
                    realClient.NetworkHandler.BroadcastMessage(new DisconnectionNotificationMessage(client.Id), realClient.SelfClient.Id);
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

        internal void RebalanceGraph(object state)
        {
            try { 
                if (realClient.SelfClient.ParentId == 0xFFFF)
                {
                    EventQueue.AddEvent(RebalanceGraph, null,
                        DateTime.Now.AddMilliseconds(realClient.Config.RebalancingTimeout), rebalanceGraphEventGuid);
                    return;
                }

                var graph = new Dictionary<ushort, List<ushort>>();
                var childrenGraph = new Dictionary<ushort, List<ushort>>();

                var root = realClient.SelfClient.Id;

                foreach (var client in realClient.ClientsById.Values.Concat(new List<DnmpNode> { realClient.SelfClient }))
                {
                    if (!graph.ContainsKey(client.Id))
                    {
                        graph.Add(client.Id, new List<ushort>());
                        childrenGraph.Add(client.Id, new List<ushort>());
                    }

                    if (client.ParentId == 0xFFFF)
                    {
                        root = client.Id;
                        continue;
                    }

                    if (!graph.ContainsKey(client.ParentId))
                        graph.Add(client.ParentId, new List<ushort>());

                    graph[client.Id].Add(client.ParentId);
                    graph[client.ParentId].Add(client.Id);

                    if (!childrenGraph.ContainsKey(client.ParentId))
                        childrenGraph.Add(client.ParentId, new List<ushort>());

                    childrenGraph[client.ParentId].Add(client.Id);
                }

                if (realClient.ClientsById[realClient.SelfClient.ParentId].ParentId != 0xFFFF)
                {
                    if (graph[realClient.SelfClient.ParentId].Count == 1 &&
                        graph[realClient.ClientsById[realClient.SelfClient.ParentId].ParentId].Count == 1)
                    {
                        ChangeClientParent(realClient.SelfClient.Id, realClient.SelfClient.ParentId);
                        realClient.NetworkHandler.BroadcastMessage(new ParentSwitchMessage(realClient.SelfClient.ParentId), realClient.SelfClient.Id);
                        EventQueue.AddEvent(RebalanceGraph, null,
                            DateTime.Now.AddMilliseconds(realClient.Config.RebalancingTimeout), rebalanceGraphEventGuid);
                        return;
                    }
                }

                var depthes = new Dictionary<ushort, ushort>();
                {
                    var bfsQueue = new Queue<dynamic>();
                    bfsQueue.Enqueue(new {
                        ClientId = root,
                        Depth = (ushort) 0
                    });
                    while (bfsQueue.Any())
                    {
                        var current = bfsQueue.Dequeue();
                        depthes.Add(current.ClientId, current.Depth);
                        foreach (var child in childrenGraph[current.ClientId])
                        {
                            bfsQueue.Enqueue(new {
                                ClientId = (ushort) child,
                                Depth = (ushort) (current.Depth + 1)
                            });
                        }
                    }
                }

                if (childrenGraph[realClient.SelfClient.Id].Count == 0 && !realClient.ClientsById.Values
                        .Concat(new List<DnmpNode> {realClient.SelfClient}).Select(x => x.Id)
                        .Where(x => graph[x].Count == 0).Any(x =>
                            depthes[x] > depthes[realClient.SelfClient.Id] ||
                            depthes[x] == depthes[realClient.SelfClient.Id] && x > realClient.SelfClient.Id))
                {
                    var bfsQueue = new Queue<ushort>();
                    bfsQueue.Enqueue(root);
                    while (bfsQueue.Any())
                    {
                        var current = bfsQueue.Dequeue();   
                        foreach (var child in childrenGraph[current])
                            bfsQueue.Enqueue(child);
                        if (current == realClient.SelfClient.Id) continue;
                        if (childrenGraph[current].Count >= 2) continue;
                        ChangeClientParent(realClient.SelfClient.Id, realClient.SelfClient.ParentId);
                        realClient.NetworkHandler.BroadcastMessage(new ParentSwitchMessage(realClient.SelfClient.ParentId), realClient.SelfClient.Id);
                        EventQueue.AddEvent(RebalanceGraph, null,
                            DateTime.Now.AddMilliseconds(realClient.Config.RebalancingTimeout), rebalanceGraphEventGuid);
                        return;
                    }
                }

                EventQueue.AddEvent(RebalanceGraph, null,
                    DateTime.Now.AddMilliseconds(realClient.Config.RebalancingTimeout), rebalanceGraphEventGuid);
            }
            catch (Exception)
            {
                if (realClient.CurrentStatus == DnmpClient.ClientStatus.Disconnecting ||
                    realClient.CurrentStatus == DnmpClient.ClientStatus.NotConnected)
                    return;
                throw;
            }
        }
        private void HeartbeatTimerCallback(object _)
        {
            try
            {
                foreach (var client in realClient.ClientsById.Values)
                    if (client.ParentId == realClient.SelfClient.Id || client.Id == realClient.SelfClient.ParentId)
                    {
                        realClient.NetworkHandler.SendBaseMessage(
                            new BaseMessage(new HeartbeatMessage(), realClient.SelfClient.Id, client.Id),
                            client.EndPoint);
                    }
            }
            catch (Exception e)
            {
                logger.Error(e, "Exception in HeartbeatTimerCallback");
            }
        }

        private void PingUpdateTimerCallback(object _)
        {
            try
            {
                var unDirectClientIds = realClient.ClientsById.Values.Where(client => !client.Flags.HasFlag(ClientFlags.DirectConnectionAvailable)).Select(client => client.Id).ToList();

                if (unDirectClientIds.Count == 0)
                    return;

                var pingUpdateMessage = new PingUpdateMessage(unDirectClientIds.ToArray());
                foreach (var client in realClient.ClientsById.Values)
                    if (client.Flags.HasFlag(ClientFlags.DirectConnectionAvailable) &&
                        client.Flags.HasFlag(ClientFlags.SymmetricKeyExchangeDone))
                        realClient.NetworkHandler.SendReliableMessage(pingUpdateMessage, realClient.SelfClient.Id, client.Id);
            }
            catch (Exception e)
            {
                logger.Error(e, "Exception in HeartbeatTimerCallback");
            }
        }

        private void DirectClientsPingTimerCallback(object _)
        {
            try
            {
                foreach (var client in realClient.ClientsById.Values)
                {
                    if ((DateTime.Now - client.LastPingReceiveTime).TotalMilliseconds > realClient.Config.MaxPingAnswerTime * 3)
                    {
                        realClient.SecondaryConnectionStop(client.Id);
                        client.Flags &= ~ClientFlags.DirectConnectionAvailable;
                        client.Flags &= ~ClientFlags.SymmetricKeyExchangeDone;
                        client.MainKey = null;
                        continue;
                    }

                    if (!client.Flags.HasFlag(ClientFlags.DirectConnectionAvailable))
                        continue;
                    client.LastPingSendTime = DateTime.Now;
                    realClient.NetworkHandler.SendBaseMessage(
                        new BaseMessage(new PingMessage(new byte[0]), realClient.SelfClient.Id, client.Id),
                        client.EndPoint);
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Exception in HeartbeatTimerCallback");
            }
        }

        public void Start()
        {
            fixingTo = null;

            rebalanceGraphEventGuid = Guid.Empty;
            reconnectionTimeoutExceededEventGuid = Guid.Empty;

            tokens.Clear();

            realClient.MessageInterface.Send = SendMessage;
            realClient.MessageInterface.Broadcast = BroadcastMessage;
            realClient.MessageInterface.HostExists = id => realClient.ClientsById.ContainsKey(id);
            realClient.MessageInterface.GetNodes = () => realClient.ClientsById.Values.ToArray();

            heartbeatTimer = new Timer(HeartbeatTimerCallback, null, 0, realClient.Config.HeartbeatDelay);
            directClientPingTimer = new Timer(DirectClientsPingTimerCallback, null, 0, realClient.Config.MaxPingAnswerTime);
            logger.Debug("MessageHandler started");
        }

        public void Stop()
        {
            heartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
            directClientPingTimer.Change(Timeout.Infinite, Timeout.Infinite);
            pingUpdateTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            EventQueue.RemoveEvent(rebalanceGraphEventGuid);
            EventQueue.RemoveEvent(reconnectionTimeoutExceededEventGuid);
            foreach (var client in realClient.ClientsById.Values)
                EventQueue.RemoveEvent(client.DisconnectEventGuid);
            logger.Debug("MessageHandler stopped");
        }

        public void Dispose()
        {
            secureRandom.Dispose();
        }
    }
}
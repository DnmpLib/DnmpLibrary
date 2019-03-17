using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DNMPLibrary.Core;
using DNMPLibrary.Handlers;
using DNMPLibrary.Interaction.MessageInterface;
using DNMPLibrary.Interaction.MessageInterface.Impl;
using DNMPLibrary.Interaction.Protocol;
using DNMPLibrary.Interaction.Protocol.ProtocolImpl;
using DNMPLibrary.Network;
using DNMPLibrary.Network.Messages;
using DNMPLibrary.Network.Messages.Types;
using DNMPLibrary.Security.Cryptography.Asymmetric;
using DNMPLibrary.Security.Cryptography.Symmetric;
using DNMPLibrary.Util;
using NLog;

namespace DNMPLibrary.Client
{
    public class DNMPClient : IDisposable
    {
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        internal NetworkHandler NetworkHandler { get; }
        internal MessageHandler MessageHandler { get; }

        public DNMPNode SelfClient { get; set; }

        public ConcurrentDictionary<ushort, DNMPNode> ClientsById { get; } = new ConcurrentDictionary<ushort, DNMPNode>();


        internal IAsymmetricKey Key { get; private set; }

        internal ISymmetricKey DummySymmetricKey { get; private set; }

        public MessageInterface MessageInterface { get; }
        
        public ClientConfig Config { get; }

        public delegate void ClientConnectedEvent(ushort clientId);
        public delegate void ClientDisconnectedEvent(ushort clientId);
        public delegate void ClientParentChangedEvent(ushort clientId, ushort newParentId, ushort oldParentId);

        public delegate void ConnectedEvent();
        public delegate void ConnectionTimeoutEvent();
        public delegate void DisconnectedEvent();

        public delegate void SecondaryConnectionStartEvent(ushort fromClientId, ushort toClientId);
        public delegate void SecondaryConnectionStopEvent(ushort fromClientId, ushort toClientId);

        public event ClientConnectedEvent OnClientConnected;
        public event ClientDisconnectedEvent OnClientDisconnected;
        public event ClientParentChangedEvent OnClientParentChange;

        public event ConnectedEvent OnConnected;
        public event ConnectionTimeoutEvent OnConnectionTimeout;
        public event DisconnectedEvent OnDisconnected;

        public event SecondaryConnectionStartEvent OnSecondaryConnectionStart;
        public event SecondaryConnectionStopEvent OnSecondaryConnectionStop;

        private Guid connectionTimeoutEvent;

        public enum ClientStatus
        {
            NotConnected,
            Connected,
            Connecting,
            Handshaking,
            Disconnecting
        }

        public ClientStatus CurrentStatus { get; internal set; }

        public DNMPClient() : this(new DummyMessageInterface()) { }

        public DNMPClient(MessageInterface messageInterface) : this(messageInterface, new UdpProtocol(), new ClientConfig()) { }

        public DNMPClient(MessageInterface messageInterface, Protocol usedProtocol, ClientConfig config)
        {
            Config = config;
            MessageInterface = messageInterface;
            MessageHandler = new MessageHandler(this);
            NetworkHandler = new NetworkHandler(this, usedProtocol);
        }

        public async Task ConnectAsync(IEndPoint endPoint, IEndPoint sourceEndPoint, bool invokeEvents, IAsymmetricKey key, ISymmetricKey dummySymmetricKey)
        {
            await ConnectManyAsync(new[] { endPoint }, sourceEndPoint, invokeEvents, key, dummySymmetricKey);
        }

        public async Task ConnectManyAsync(IEndPoint[] endPoints, IEndPoint sourceEndPoint, bool invokeEvents, IAsymmetricKey key, ISymmetricKey dummySymmetricKey)
        {
            if (CurrentStatus != ClientStatus.NotConnected)
                return;
            Initialize(sourceEndPoint, key, dummySymmetricKey);
            CurrentStatus = ClientStatus.Connecting;
            if (invokeEvents)
            {
                connectionTimeoutEvent = EventQueue.AddEvent(_ =>
                {
                    NetworkHandler.Stop();
                    MessageHandler.Stop();
                    ClientsById.Clear();
                    CurrentStatus = ClientStatus.NotConnected;
                    OnConnectionTimeout?.Invoke();
                }, null, DateTime.Now.AddMilliseconds(Config.ConnectionTimeout));
            }
            else
            {
                connectionTimeoutEvent = EventQueue.AddEvent(_ =>
                {
                    CurrentStatus = ClientStatus.NotConnected;
                }, null, DateTime.Now.AddMilliseconds(Config.ConnectionTimeout));
            }

            logger.Debug($"Trying to connect to {endPoints.Length} endpoints. First: {endPoints.FirstOrDefault()}");

            foreach (var endPoint in endPoints)
            {
                logger.Debug($"Trying to connect to {endPoint}");
                NetworkHandler.SendBaseMessage(new BaseMessage(new ConnectionRequestMessage(key.GetNetworkId(), true), 0xFFFF, 0xFFFF),
                    endPoint);
            }

            if (invokeEvents)
                return;
            SpinWait.SpinUntil(() => CurrentStatus == ClientStatus.Connecting || CurrentStatus == ClientStatus.Handshaking);
            if (CurrentStatus == ClientStatus.NotConnected)
                throw new TimeoutException("Connection timeout");
            await Task.Delay(0);
        }

        public async Task StartAsFirstNodeAsync(IEndPoint sourceEndPoint, IEndPoint publicEndPoint, IAsymmetricKey key, ISymmetricKey dummySymmetricKey)
        {
            if (CurrentStatus != ClientStatus.NotConnected)
                return;
            SelfClient = new DNMPNode
            {
                Id = 0,
                EndPoint = publicEndPoint
            };
            Initialize(sourceEndPoint, key, dummySymmetricKey);
            CurrentStatus = ClientStatus.Connected;
            logger.Info($"Started as first node on {sourceEndPoint} [{publicEndPoint}]");
            OnClientConnected?.Invoke(SelfClient.Id);
            OnConnected?.Invoke();
            MessageInterface.Initialize(SelfClient.Id);
            await Task.Delay(0);
        }
        
        private void Initialize(IEndPoint sourceEndPoint, IAsymmetricKey key, ISymmetricKey dummySymmetricKey)
        {
            Key = key;
            DummySymmetricKey = dummySymmetricKey;
            MessageHandler.Start();
            NetworkHandler.Start(sourceEndPoint);
        }

        internal void AddClient(DNMPNode client)
        {
            logger.Debug($"Added client #{client.Id}");
            ClientsById.TryAdd(client.Id, client);
            OnClientConnected?.Invoke(client.Id);
        }

        internal void RemoveClient(DNMPNode client)
        {
            logger.Debug($"Removed client #{client.Id}");
            ClientsById.TryRemove(client.Id, out _);
            OnClientDisconnected?.Invoke(client.Id);
        }
        
        internal void ClientParentChanged(ushort clientId, ushort newParentId, ushort oldParentId) =>
            OnClientParentChange?.Invoke(clientId, newParentId, oldParentId);

        internal void SecondaryConnectionStart(ushort toClientId) =>
            OnSecondaryConnectionStart?.Invoke(SelfClient.Id, toClientId);

        internal void SecondaryConnectionStop(ushort toClientId) =>
            OnSecondaryConnectionStop?.Invoke(SelfClient.Id, toClientId);


        internal void SelfConnected()
        {
            CurrentStatus = ClientStatus.Connected;
            EventQueue.RemoveEvent(connectionTimeoutEvent);
            OnConnected?.Invoke();
        }

        public void Stop()
        {
            if (CurrentStatus == ClientStatus.NotConnected)
                return;
            CurrentStatus = ClientStatus.Disconnecting;
            EventQueue.RemoveEvent(connectionTimeoutEvent);
            NetworkHandler.Stop();
            MessageHandler.Stop();
            ClientsById.Clear();
            CurrentStatus = ClientStatus.NotConnected;
            OnDisconnected?.Invoke();
        }
        
        public void Dispose()
        {
            MessageHandler.Dispose();
        }
    }
}

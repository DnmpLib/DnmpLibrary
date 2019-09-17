using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DnmpLibrary.Core;
using DnmpLibrary.Handlers;
using DnmpLibrary.Interaction.MessageInterface;
using DnmpLibrary.Interaction.MessageInterface.Impl;
using DnmpLibrary.Interaction.Protocol;
using DnmpLibrary.Interaction.Protocol.ProtocolImpl;
using DnmpLibrary.Network;
using DnmpLibrary.Network.Messages;
using DnmpLibrary.Network.Messages.Types;
using DnmpLibrary.Security.Cryptography.Asymmetric;
using DnmpLibrary.Security.Cryptography.Symmetric;
using DnmpLibrary.Util;
using NLog;

namespace DnmpLibrary.Client
{
    public class DnmpClient : IDisposable
    {
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        internal NetworkHandler NetworkHandler { get; }
        internal MessageHandler MessageHandler { get; }

        public DnmpNode SelfClient { get; set; }

        public ConcurrentDictionary<ushort, DnmpNode> ClientsById { get; } = new ConcurrentDictionary<ushort, DnmpNode>();


        internal IAsymmetricKey Key { get; private set; }

        internal ISymmetricKey DummySymmetricKey { get; private set; }

        internal byte[] SelfCustomData { get; private set; }

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

        public DnmpClient() : this(new DummyMessageInterface()) { }

        public DnmpClient(MessageInterface messageInterface) : this(messageInterface, new UdpProtocol(), new ClientConfig()) { }

        public DnmpClient(MessageInterface messageInterface, Protocol usedProtocol, ClientConfig config)
        {
            Config = config;
            MessageInterface = messageInterface;
            MessageHandler = new MessageHandler(this);
            NetworkHandler = new NetworkHandler(this, usedProtocol);
        }

        public async Task ConnectAsync(IEndPoint endPoint, IEndPoint sourceEndPoint, bool invokeEvents, IAsymmetricKey key, ISymmetricKey dummySymmetricKey, byte[] selfCustomData)
        {
            await ConnectManyAsync(new[] { endPoint }, sourceEndPoint, invokeEvents, key, dummySymmetricKey, selfCustomData);
        }

        public async Task ConnectManyAsync(IEndPoint[] endPoints, IEndPoint sourceEndPoint, bool invokeEvents, IAsymmetricKey key, ISymmetricKey dummySymmetricKey, byte[] selfCustomData)
        {
            if (CurrentStatus != ClientStatus.NotConnected)
                return;
            if (selfCustomData.Length > 65000)
                throw new DnmpException("Custom data length is larger than 65000 bytes");
            SelfCustomData = selfCustomData;
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
                try
                {
                    NetworkHandler.SendBaseMessage(
                        new BaseMessage(new ConnectionRequestMessage(key.GetNetworkId(), true), 0xFFFF, 0xFFFF),
                        endPoint);
                }
                catch (Exception e)
                {
                    logger.Warn($"Caught exception while trying to connect: {e.GetType().Name}('{e.Message}')");
                }
            }

            if (invokeEvents)
                return;
            SpinWait.SpinUntil(() => CurrentStatus == ClientStatus.Connecting || CurrentStatus == ClientStatus.Handshaking);
            if (CurrentStatus == ClientStatus.NotConnected)
                throw new TimeoutException("Connection timeout");
            await Task.Delay(0);
        }

        public async Task StartAsFirstNodeAsync(IEndPoint sourceEndPoint, IEndPoint publicEndPoint, IAsymmetricKey key, ISymmetricKey dummySymmetricKey, byte[] selfCustomData)
        {
            if (CurrentStatus != ClientStatus.NotConnected)
                return;
            if (selfCustomData.Length > 65000) //TODO
                throw new DnmpException("Custom data length is larger than 65000 bytes");
            SelfCustomData = selfCustomData;
            SelfClient = new DnmpNode
            {
                Id = 0,
                EndPoint = publicEndPoint,
                CustomData = SelfCustomData
            };
            Initialize(sourceEndPoint, key, dummySymmetricKey);
            CurrentStatus = ClientStatus.Connected;
            logger.Info($"Started as first node on {sourceEndPoint} [{publicEndPoint}]");
            OnClientConnected?.Invoke(SelfClient.Id);
            OnConnected?.Invoke();
            await MessageInterface.Initialize(SelfClient.Id);
            await Task.Delay(0);
        }
        
        private void Initialize(IEndPoint sourceEndPoint, IAsymmetricKey key, ISymmetricKey dummySymmetricKey)
        {
            Key = key;
            DummySymmetricKey = dummySymmetricKey;
            MessageHandler.Start();
            NetworkHandler.Start(sourceEndPoint);
        }

        internal void AddClient(DnmpNode client)
        {
            logger.Debug($"Added client #{client.Id}");
            ClientsById.TryAdd(client.Id, client);
            OnClientConnected?.Invoke(client.Id);
        }

        internal void RemoveClient(DnmpNode client)
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

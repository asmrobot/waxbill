using System;
using System.Runtime.CompilerServices;
using System.Threading;
using waxbill.Packets;
using waxbill.Pools;
using waxbill.Sessions;
using waxbill.Utils;



namespace waxbill
{
    public abstract class SocketMonitor
    {
        private OnConnectionEvent s;
        private long connectionIncremer;        
        internal SocketConfiguration Config;
        
        public OnConnectionEvent OnConnection;
        public OnDisconnectedEvent OnDisconnected;
        public OnReceiveEvent OnReceive;
        public OnSendedEvent OnSended;
        
        internal SendingQueuePool SendingPool;
        internal EventArgPool SocketEventArgsPool;
        internal ReceiveBufferPool ReceiveBufferPool;
        internal PacketBufferPool PacketBufferPool;



        public IProtocol Protocol { get; set; }

        public int BufferSize { get; private set; }



        public SocketMonitor(IProtocol protocol, SocketConfiguration config)
        {
            Preconditions.ThrowIfNull(protocol, "protocol");
            Preconditions.ThrowIfNull(config, "config");

            this.Protocol = protocol;
            this.Config = config;
            this.SendingPool = new SendingQueuePool(config.SizeOfSendQueue,config.IncreasesOfSendingQueuePool, config.MaxOfSendingQueuePool);
            this.SocketEventArgsPool = new EventArgPool(config.IncreasesOfEventArgPool,config.MaxOfClient<=0?0:config.MaxOfClient*2);
            this.ReceiveBufferPool = new ReceiveBufferPool(config.BufferSizeOfReceiveBufferPool,config.IncreasesOfReceiveBufferPool,config.MaxOfReceiveBufferPool);
            this.PacketBufferPool = new PacketBufferPool(config.BufferSizeOfPacketBufferPool, config.IncreasesOfPacketBufferPool,config.MaxOfPacketBufferPool);
        }

        internal long GetNextConnectionID()
        {
            return Interlocked.Increment(ref this.connectionIncremer);
        }

        internal void RaiseOnConnectedEvent(SessionBase session)
        {
            if (this.OnConnection != null)
            {
                this.OnConnection(session);
            }
        }

        internal void RaiseOnDisconnectedEvent(SessionBase session, CloseReason reason)
        {
            if (this.OnDisconnected != null)
            {
                this.OnDisconnected(session, reason);
            }
        }

        internal void RaiseOnReceivedEvent(SessionBase session, Packet collection)
        {
            if (this.OnReceive != null)
            {
                this.OnReceive(session, collection);
            }
        }

        internal void RaiseOnSendedEvent(SessionBase session, SendingQueue packet, bool result)
        {
            if (this.OnSended != null)
            {
                this.OnSended(session, packet, result);
            }
        }
    }
}


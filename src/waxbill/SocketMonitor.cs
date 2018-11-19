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
    }
}


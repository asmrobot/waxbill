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

        private long connectionCounter;
        /// <summary>
        /// 连接计数器
        /// </summary>
        public long ConnectionCounter
        {
            get
            {
                return this.connectionCounter;
            }
        }

        /// <summary>
        /// 发送队列池
        /// </summary>
        internal SendingQueuePool SendingPool
        {
            get
            {
                return this.PoolProvider.SendingPool;
            }
        }
        
        /// <summary>
        /// 发送和接收SocketAsyncEventArgs池
        /// </summary>
        internal EventArgPool SocketEventArgsPool
        {
            get
            {
                return this.PoolProvider.SocketEventArgsPool;
            }
        }

        /// <summary>
        /// 接收数据缓存池
        /// </summary>
        internal ReceiveBufferPool ReceiveBufferPool
        {
            get
            {
                return this.PoolProvider.ReceiveBufferPool;
            }
        }

        /// <summary>
        /// 包存储缓存池
        /// </summary>
        internal PacketBufferPool PacketBufferPool
        {
            get
            {
                return this.PoolProvider.PacketBufferPool;
            }
        }

        /// <summary>
        /// 使用协议
        /// </summary>
        public IProtocol Protocol { get; private set; }

        public PoolProvider PoolProvider { get; private set; }

        public TCPOption Option { get; private set; }

        public SocketMonitor(IProtocol protocol, TCPOption config, PoolProvider poolProvider)
        {
            Preconditions.ThrowIfNull(protocol, "protocol");
            Preconditions.ThrowIfNull(config, "config");

            this.Protocol = protocol;
            this.Option = config;
            this.PoolProvider = poolProvider;
        }



        internal long GetNextConnectionID()
        {
            return Interlocked.Increment(ref this.connectionCounter);
        }
    }
}


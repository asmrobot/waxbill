using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace waxbill.Pools
{
    /// <summary>
    /// 池提供程序
    /// </summary>
    public class PoolProvider
    {
        public SendingQueuePool SendingPool;
        public EventArgPool SocketEventArgsPool;
        public ReceiveBufferPool ReceiveBufferPool;
        public PacketBufferPool PacketBufferPool;


        public PoolProvider(TCPOption config)
        {
            this.SendingPool = new SendingQueuePool(config.SizeOfSendQueue, config.IncreasesOfSendingQueuePool, config.MaxOfSendingQueuePool);
            this.SocketEventArgsPool = new EventArgPool(config.IncreasesOfEventArgPool, config.MaxOfClient <= 0 ? 0 : config.MaxOfClient * 2);
            this.ReceiveBufferPool = new ReceiveBufferPool(config.BufferSizeOfReceiveBufferPool, config.IncreasesOfReceiveBufferPool, config.MaxOfReceiveBufferPool);
            this.PacketBufferPool = new PacketBufferPool(config.BufferSizeOfPacketBufferPool, config.IncreasesOfPacketBufferPool, config.MaxOfPacketBufferPool);
        }
    }
}

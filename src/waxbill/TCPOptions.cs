namespace waxbill
{
    using System;
    using System.Runtime.CompilerServices;

    public class TCPOptions

    {
        /// <summary>
        /// 默认服务器配置
        /// </summary>
        public static readonly TCPOptions SERVER_DEFAULT;

        /// <summary>
        /// 默认客户端配置
        /// </summary>
        public static readonly TCPOptions CLIENT_DEFAULT;


        static TCPOptions()
        {
            
            SERVER_DEFAULT = new TCPOptions
            {

                AutoRecycleSession = true,
                RecycleSecond = 30000,
                SendTimeout = 10000,

                MaxBlockSize = 500,

                IncreasesOfEventArgPool = 10,
                MaxOfClient = 0,

                BufferSizeOfReceiveBufferPool = 1024,//每个接收缓冲区的大小
                IncreasesOfReceiveBufferPool = 10,
                MaxOfReceiveBufferPool = 0,


                BufferSizeOfPacketBufferPool = 1024,
                IncreasesOfPacketBufferPool = 1000,
                MaxOfPacketBufferPool = 0,

                IncreasesOfSendingQueuePool = 1000,
                MaxOfSendingQueuePool = 0,
                SizeOfSendQueue = 6,
            };

            CLIENT_DEFAULT = new TCPOptions
            {
                AutoRecycleSession = true,
                RecycleSecond = 30000,
                SendTimeout = 10000,

                MaxBlockSize = 500,

                IncreasesOfEventArgPool = 10,
                MaxOfClient = 0,

                BufferSizeOfReceiveBufferPool = 1024,//每个接收缓冲区的大小
                IncreasesOfReceiveBufferPool = 10,
                MaxOfReceiveBufferPool = 0,


                BufferSizeOfPacketBufferPool = 1024,
                IncreasesOfPacketBufferPool = 1000,
                MaxOfPacketBufferPool = 0,

                IncreasesOfSendingQueuePool = 1000,
                MaxOfSendingQueuePool = 0,
                SizeOfSendQueue = 6,
            };
        }

        /// <summary>
        /// 是否自动回收会话
        /// </summary>
        public bool AutoRecycleSession { get; set; }

        /// <summary>
        /// 回收秒数，毫秒
        /// </summary>
        public int RecycleSecond { get; set; }

        /// <summary>
        /// Socket监听时最长等待队列
        /// </summary>
        public int MaxBlockSize { get; set; }
        
        /// <summary>
        /// 发送超时时长(毫秒)
        /// </summary>
        public int SendTimeout { get; set; }

        /// <summary>
        /// 最大客户数
        /// </summary>
        public int MaxOfClient { get; set; }


        /// <summary>
        /// SocketAsyncEventArgsbn每次建议增加量
        /// </summary>
        public Int32 IncreasesOfEventArgPool { get; set; }

        


        #region receive
        /// <summary>
        /// 接收池最大数量
        /// </summary>
        public int MaxOfReceiveBufferPool { get; set; }

        /// <summary>
        /// 每次增加量
        /// </summary>
        public Int32 IncreasesOfReceiveBufferPool { get; set; }

        /// <summary>
        /// 每个接收缓存的大小
        /// </summary>
        public int BufferSizeOfReceiveBufferPool { get; set; }

        #endregion

        #region packet
        /// <summary>
        /// 包池最大数量
        /// </summary>
        public Int32 MaxOfPacketBufferPool { get; set; }

        /// <summary>
        /// 包池增量
        /// </summary>
        public Int32 IncreasesOfPacketBufferPool { get; set; }

        /// <summary>
        /// 包池单项大小
        /// </summary>
        public int BufferSizeOfPacketBufferPool { get; set; }
        #endregion


        #region send
        /// <summary>
        /// 发送队列池每次建议增加数量 
        /// </summary>
        public int IncreasesOfSendingQueuePool { get; set; }

        /// <summary>
        /// 发送队列池最大发送队列数量
        /// </summary>
        public int MaxOfSendingQueuePool { get; set; }

        /// <summary>
        /// 发送队列大小
        /// </summary>
        public int SizeOfSendQueue { get; set; }
        #endregion
    }
}


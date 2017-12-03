using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace waxbill
{
    public class ServerOption
    {
        public static readonly ServerOption Define;
        static ServerOption()
        {
            Define = new ServerOption() {
                ListenBacklog = 100,
                BufferSize = 8192,
                BufferIncemerCount = 1000,
                ReceiveBufferSize = 40960,
                MaxMessageSize = 10000000,

                MinSendingPoolSize = 256,
                MaxSendingPoolSize = 200000,
                SendQueueSize = 6,
                SendTimeout = 5000,
                AutoRecycleSession = true,
                RecycleSessionFrequency = 5000,

                MaxSocketPoolSize = 100
            };
        }

        /// <summary>
        /// 监听队列长度
        /// </summary>
        public Int32 ListenBacklog { get; set; }




        /// <summary>
        /// 缓存大小
        /// </summary>
        public int BufferSize
        {
            get; set;
        }

        /// <summary>
        /// 缓存递增数量
        /// </summary>
        public int BufferIncemerCount
        {
            get; set;
        }



        /// <summary>
        /// 接收缓存大小
        /// </summary>
        public int ReceiveBufferSize
        {
            get; set;
        }

        /// <summary>
        /// 最大消息大小
        /// </summary>
        public int MaxMessageSize
        {
            get;
            set;
        }

        /// <summary>
        /// 最大接收缓存池大小
        /// </summary>
        public int MaxReceivePoolSize
        {
            get;
            set;
        }

        /// <summary>
        /// 发送池最小个数
        /// </summary>
        public int MinSendingPoolSize
        {
            get; set;
        }

        /// <summary>
        /// 发送池最大个数，小于等于0为不限
        /// </summary>
        public int MaxSendingPoolSize
        {
            get; set;
        }

        /// <summary>
        /// 发送队列长度
        /// </summary>
        public int SendQueueSize
        {
            get; set;
        }

        /// <summary>
        /// 发送超时时长，毫秒
        /// </summary>
        public int SendTimeout
        {
            get;
            set;
        }

        /// <summary>
        /// 自动回收断开的会话
        /// </summary>
        public bool AutoRecycleSession
        {
            get;
            set;
        }

        /// <summary>
        /// 回收会话周期时长
        /// 毫秒
        /// </summary>
        public Int32 RecycleSessionFrequency
        {
            get;
            set;
        }

        /// <summary>
        /// 最大SocketAsyncEventArgs池数量
        /// </summary>
        public Int32 MaxSocketPoolSize
        {
            get;
            set;
        }

    }
}

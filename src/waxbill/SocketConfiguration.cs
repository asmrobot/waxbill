namespace waxbill
{
    using System;
    using System.Runtime.CompilerServices;

    public class SocketConfiguration
    {
        public static readonly SocketConfiguration Default;

        static SocketConfiguration()
        {
            SocketConfiguration configuration1 = new SocketConfiguration {
                BufferSize = 0x2000,
                BufferIncemerCount = 0x3e8,
                ReceiveBufferSize = 0xa000,
                MaxMessageSize = 0x989680,
                MinSendingPoolSize = 0x100,
                MaxSendingPoolSize = 0x30d40,
                SendQueueSize = 6,
                SendTimeout = 0x1388,
                AutoRecycleSession = true,
                RecycleSessionFrequency = 0x1388,
                MaxSocketPoolSize = 100,
                MaxBlockSize = 500
            };
            Default = configuration1;
        }

        public bool AutoRecycleSession { get; set; }

        public int BufferIncemerCount { get; set; }

        public int BufferSize { get; set; }

        public int MaxBlockSize { get; set; }

        public int MaxMessageSize { get; set; }

        public int MaxReceivePoolSize { get; set; }

        public int MaxSendingPoolSize { get; set; }

        public int MaxSocketPoolSize { get; set; }

        public int MinSendingPoolSize { get; set; }

        public int ReceiveBufferSize { get; set; }

        public int RecycleSessionFrequency { get; set; }

        public int SendQueueSize { get; set; }

        public int SendTimeout { get; set; }
    }
}


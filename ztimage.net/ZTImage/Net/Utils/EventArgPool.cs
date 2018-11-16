namespace ZTImage.Net.Utils
{
    using System;
    using System.Collections.Concurrent;
    using System.Net.Sockets;
    using ZTImage.Net;

    public class EventArgPool
    {
        private readonly ConcurrentStack<SocketAsyncEventArgs> _PoolStack = new ConcurrentStack<SocketAsyncEventArgs>();
        private SocketConfiguration m_Config;

        public EventArgPool(SocketConfiguration config)
        {
            Preconditions.CheckNotNull<SocketConfiguration>(config, "config");
            this.m_Config = config;
            if (this.m_Config.MaxSocketPoolSize <= 0)
            {
                this.m_Config.MaxSocketPoolSize = 0x7fffffff;
            }
        }

        internal SocketAsyncEventArgs GetSocketAsyncEventArgs()
        {
            SocketAsyncEventArgs args;
            if (!this._PoolStack.TryPop(out args))
            {
                args = new SocketAsyncEventArgs();
                byte[] buffer = new byte[this.m_Config.ReceiveBufferSize];
                args.SetBuffer(buffer, 0, buffer.Length);
            }
            return args;
        }

        internal void RealseSocketAsyncEventArgs(SocketAsyncEventArgs e)
        {
            if ((e.Buffer == null) || (e.Buffer.Length != this.m_Config.ReceiveBufferSize))
            {
                e.Dispose();
            }
            else if (this._PoolStack.Count >= this.m_Config.MaxSocketPoolSize)
            {
                e.SetBuffer(null, 0, 0);
                e.Dispose();
            }
            else
            {
                this._PoolStack.Push(e);
            }
        }
    }
}


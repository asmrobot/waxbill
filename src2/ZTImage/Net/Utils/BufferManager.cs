namespace ZTImage.Net.Utils
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using ZTImage.Net;

    public class BufferManager
    {
        private object _BufferLocker = new object();
        private List<byte[]> m_Buffers = new List<byte[]>();
        private SocketConfiguration m_Config;
        private int m_CurrentIndex;
        private ConcurrentStack<ArraySegment<byte>> m_freeIndexPool;
        private int m_ListIndex;

        public BufferManager(SocketConfiguration config)
        {
            Preconditions.CheckNotNull<SocketConfiguration>(config, "config");
            this.m_Config = config;
            this.m_ListIndex = -1;
            this.m_CurrentIndex = 0;
            this.m_freeIndexPool = new ConcurrentStack<ArraySegment<byte>>();
        }

        public void FreeBuffer(ArraySegment<byte> datas)
        {
            object obj2 = this._BufferLocker;
            lock (obj2)
            {
                if (datas.Count == this.m_Config.BufferSize)
                {
                    this.m_freeIndexPool.Push(datas);
                }
            }
        }

        public void FreeBuffer(byte[] datas, int offset)
        {
            this.FreeBuffer(new ArraySegment<byte>(datas, offset, 0));
        }

        public ArraySegment<byte> GetBuffer()
        {
            object obj2 = this._BufferLocker;
            lock (obj2)
            {
                ArraySegment<byte> segment;
                if (!this.m_freeIndexPool.TryPop(out segment))
                {
                    if ((this.m_ListIndex < 0) || (this.m_CurrentIndex > ((this.m_Config.BufferIncemerCount - 1) * this.m_Config.BufferSize)))
                    {
                        this.m_Buffers.Add(new byte[this.m_Config.BufferSize * this.m_Config.BufferIncemerCount]);
                        this.m_ListIndex++;
                        this.m_CurrentIndex = 0;
                    }
                    segment = new ArraySegment<byte>(this.m_Buffers[this.m_ListIndex], this.m_CurrentIndex, this.m_Config.BufferSize);
                    this.m_CurrentIndex += this.m_Config.BufferSize;
                }
                return segment;
            }
        }

        public int BufferIncemerCount
        {
            get
            {
                return this.m_Config.BufferIncemerCount;
            }
        }

        public int BufferSize
        {
            get
            {
                return this.m_Config.BufferSize;
            }
        }
    }
}


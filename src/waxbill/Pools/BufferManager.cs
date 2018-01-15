using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace waxbill.Utils
{
    public class BufferManager
    {
        private List<byte[]> m_Buffers=new List<byte[]>(); 
        
        private ConcurrentStack<ArraySegment<byte>> m_freeIndexPool; 
        private int m_ListIndex;
        private int m_CurrentIndex;


        private int m_BufferSize = 0;//单个缓存大小
        public int BufferSize
        {
            get
            {
                return m_BufferSize;
            }
        }

        private int m_BufferIncemerCount = 0;//每次分配缓存数组，缓存增长量
        public int BufferIncemerCount
        {
            get
            {
                return m_BufferIncemerCount;
            }
        }
        private object _BufferLocker = new object();
        
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BufferManager"/> class.
        /// </summary>
        /// <param name="totalBytes">The total bytes.</param>
        /// <param name="bufferSize">Size of the buffer.</param>
        public BufferManager(int bufferSize,int bufferIncemerCount)
        {
            if (bufferSize <= 0 )
            {
                throw new ArgumentException("bufferSize must >0");
            }
            this.m_BufferSize = bufferSize;
            this.m_BufferIncemerCount = bufferIncemerCount;


            m_ListIndex = -1;
            m_CurrentIndex = 0;   
            m_freeIndexPool = new ConcurrentStack<ArraySegment<byte>>();
        }
        
        /// <summary>
        /// 得到缓存
        /// </summary>
        /// <returns></returns>
        public ArraySegment<byte> GetBuffer()
        {
            ArraySegment<byte> data;
            lock (_BufferLocker)
            {
                if (m_freeIndexPool.TryPop(out data))
                {
                    return data;
                }
                else
                {
                    if (this.m_ListIndex < 0 || m_CurrentIndex > (this.m_BufferIncemerCount - 1) * this.m_BufferSize)
                    {
                        this.m_Buffers.Add(new byte[this.m_BufferSize * this.m_BufferIncemerCount]);
                        this.m_ListIndex++;
                        m_CurrentIndex = 0;
                    }

                    data = new ArraySegment<byte>(this.m_Buffers[this.m_ListIndex], this.m_CurrentIndex, this.m_BufferSize);
                    m_CurrentIndex += this.m_BufferSize;
                    return data;
                }
            }
        }

        /// <summary>
        /// Removes the buffer from a SocketAsyncEventArg object.  This frees the buffer back to the 
        /// buffer pool
        /// </summary>
        public void FreeBuffer(byte[] datas,Int32 offset)
        {
            FreeBuffer(new ArraySegment<byte>(datas, offset, 0));
        }

        public void FreeBuffer(ArraySegment<byte> datas)
        {
            lock (_BufferLocker)
            {
                if (datas.Count == this.m_BufferSize)
                {
                    m_freeIndexPool.Push(datas);
                } 
            }
        }
        
    }
}

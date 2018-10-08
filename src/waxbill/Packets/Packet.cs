using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using waxbill.Pools;
using waxbill.Utils;

namespace waxbill.Packets
{
    /// <summary>
    /// 缓存集合
    /// </summary>
    public class Packet:IDisposable
    {
        
        private List<ArraySegment<byte>> m_Datas;//内部数据存储        
        private BufferManager m_BufferManager;//缓存管理器        
        private int m_ListIndex;//当前列表索引
        private int m_CurrentPosition;//最后一项的位置
        private bool m_IsStart;
        private Int32 m_Count;

        private Int32 m_ForecastSize = 0;

        /// <summary>
        /// 是否开始
        /// </summary>
        internal bool IsStart
        {
            get
            {
                return m_IsStart;
            }
            set
            {
                m_IsStart = value;
            }
        }


        /// <summary>
        /// 总数据大小
        /// </summary>
        public Int64 Count
        {
            get
            {
                return this.m_Count;
            }
        }



        /// <summary>
        /// 预测包大小
        /// </summary>
        internal Int32 ForecastSize
        {
            get
            {
                return m_ForecastSize;
            }
            set
            {
                m_ForecastSize = value;
            }
        }
        
        public Packet(BufferManager bufferManager)
        {
            this.m_Count = 0;
            this.m_BufferManager = bufferManager;
            this.m_Datas = new List<ArraySegment<byte>>();
            this.m_ListIndex = -1;
            this.m_CurrentPosition = 0;
        }
        
        /// <summary>
        /// todo:得到所有数据，慎用，会增大GC负担
        /// </summary>
        public byte[] Read()
        {
            byte[] datas = new byte[this.m_Count];
            Read(datas,0, 0, datas.Length);
            return datas;
        }

        public Int32 Read(int bufferOffset, byte[] buffer, int offset, int count)
        {
            return Read(buffer, bufferOffset, offset, count);
        }

        /// <summary>
        /// 从包中读取数据
        /// </summary>
        /// <param name="buffer">目标数组</param>
        /// <param name="bufferOffset">目标数组偏移</param>
        /// <param name="offset">当前包的开始读取位置</param>
        /// <param name="count">读取数量</param>
        public Int32 Read(byte[] buffer, int bufferOffset, int offset, int count)
        {
            if (bufferOffset > this.m_Count)
            {
                return 0;
            }
            if (buffer == null)
            {
                throw new ArgumentNullException("提供的空间为空");
            }
            
            if (offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("没有提供足够大的空间，装填数据");
            }
            
            //读取开始数据和开始偏移
            int readArrayIndex = bufferOffset / this.m_BufferManager.BufferSize;//当前数组
            int readPosition = bufferOffset%this.m_BufferManager.BufferSize;//开始字节


            int canReadCount = Math.Min(count, this.m_Count - bufferOffset);
            int let = canReadCount;
            ArraySegment<byte> temp;
            while (let > 0)
            {
                if (readPosition >= this.m_BufferManager.BufferSize)
                {
                    readArrayIndex++;
                    readPosition = 0;
                }
                int copySize = Math.Min(this.m_BufferManager.BufferSize - readPosition, let);
                temp = this.m_Datas[readArrayIndex];
                Buffer.BlockCopy(temp.Array, temp.Offset + readPosition, buffer, offset + canReadCount - let, copySize);
                let -= copySize;
                readPosition += copySize;
            }
            return canReadCount;
        }

        /// <summary>
        /// 添加字节段
        /// </summary>
        /// <param name="bytes"></param>
        public void Write(IntPtr bytes,Int32 count)
        {
            if (bytes!=IntPtr.Zero&&count>0)
            {
                EnsureBuffer();
                int idleCount = 0;
                int copySize = 0;
                int let = count;//剩余copy字节数
                while (let > 0)
                {
                    idleCount = m_BufferManager.BufferSize - this.m_CurrentPosition;//当前缓存空闲
                    copySize = Math.Min(let, idleCount);
                    var temp = this.m_Datas[m_ListIndex];
                    Marshal.Copy(bytes,  temp.Array, temp.Offset + this.m_CurrentPosition, copySize);
                    bytes += copySize;
                    this.m_CurrentPosition += copySize;
                    let -= copySize;
                    if (let <= 0)
                    {
                        break;
                    }
                    EnsureBuffer();
                }
                this.m_Count += count;
            }
        }

        /// <summary>
        /// 添加字节段
        /// </summary>
        /// <param name="bytes"></param>
        internal void Write(ArraySegment<byte> bytes)
        {
            if (bytes!=default(ArraySegment<byte>)&&bytes.Count > 0)
            {
                EnsureBuffer();
                int idleCount = 0;
                int copySize = 0;
                int let = bytes.Count;//剩余copy字节数
                while (let>0)
                {
                    idleCount = m_BufferManager.BufferSize - this.m_CurrentPosition;//当前缓存空闲
                    copySize = Math.Min(let, idleCount);
                    var temp = this.m_Datas[m_ListIndex];
                    Buffer.BlockCopy(bytes.Array, bytes.Offset + bytes.Count - let, temp.Array, temp.Offset+this.m_CurrentPosition, copySize);
                    this.m_CurrentPosition += copySize;
                    let -= copySize;
                    if (let <=0)
                    {
                        break;
                    }
                    EnsureBuffer();
                }
                this.m_Count += bytes.Count;
            }
        }

        private void EnsureBuffer()
        {
            if (this.m_ListIndex < 0)
            {
                this.m_Datas.Add(this.m_BufferManager.GetBuffer());
                this.m_ListIndex = 0;
                this.m_CurrentPosition = 0;
            }

            if (this.m_CurrentPosition >= this.m_BufferManager.BufferSize)
            {
                this.m_Datas.Add(this.m_BufferManager.GetBuffer());
                this.m_ListIndex++;
                this.m_CurrentPosition = 0;
            }
        }
        
        /// <summary>
        /// 索引字节
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public byte this[Int32 index]
        {
            get
            {
                if (index >= this.m_Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                int listoffset = index / this.m_BufferManager.BufferSize;
                int offset = index % this.m_BufferManager.BufferSize;
                var segment = this.m_Datas[listoffset];
                return segment.Array[segment.Offset + offset];
            }
            set
            {
                if (index >= this.m_Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                int listoffset = index / this.m_BufferManager.BufferSize;
                int offset = index % this.m_BufferManager.BufferSize;
                var segment = this.m_Datas[listoffset];
                segment.Array[segment.Offset + offset] = value;

            }
        }

        #region Dispose
        private Int32 mIsFree = 0;
        /// <summary>
        /// 清空包
        /// </summary>
        private void Dispose(bool isFinilize)
        {
            if (Interlocked.CompareExchange(ref mIsFree, 1, 0) == 0)
            {
                Reset();
                if (!isFinilize)
                {
                    GC.SuppressFinalize(this);
                }
            }
            
            
        }

        /// <summary>
        /// 重置包
        /// </summary>
        public virtual void Reset()
        {
            for (int i = 0; i < this.m_Datas.Count; i++)
            {
                this.m_BufferManager.FreeBuffer(this.m_Datas[i]);
            }

            this.m_Datas.Clear();
            this.m_ForecastSize = 0;
            this.m_Count = 0;
            this.m_IsStart = false;
            this.m_ListIndex = -1;
            this.m_CurrentPosition = 0;
        }

        public void Dispose()
        {
            Dispose(false);
        }

        ~Packet()
        {
            Dispose(true);
        }
        #endregion
    }
}

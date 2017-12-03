using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace waxbill.Utils
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
        public Int32 Count
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


        /// <summary>
        /// 字节段个数
        /// </summary>
        public Int32 ItemCount
        {
            get
            {
                return this.m_Datas.Count;
            }
        }
        

        internal Packet(BufferManager bufferManager)
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
            Read(0, datas, 0, datas.Length);
            return datas;
        }

        /// <summary>
        /// todo:复制一个范围
        /// </summary>
        /// <param name="targetDatas">读取到的目录数组</param>
        /// <param name="offset">当前包的开始读取位置</param>
        /// <param name="count">读取数量</param>
        public Int32 Read(int sourceOffset,byte[] targetDatas, int offset, int count)
        {
            if (sourceOffset > this.m_Count)
            {
                return 0;
            }
            if (targetDatas == null)
            {
                throw new ArgumentNullException("提供的空间为空");
            }
            
            if (offset + count > targetDatas.Length)
            {
                throw new ArgumentOutOfRangeException("没有提供足够大的空间，装填数据");
            }
            
            //读取开始数据和开始偏移
            int readArrayIndex = sourceOffset / this.m_BufferManager.BufferSize;//当前数组
            int readPosition = sourceOffset%this.m_BufferManager.BufferSize;//开始字节


            int canReadCount = Math.Min(count, this.m_Count - sourceOffset);
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
                Buffer.BlockCopy(temp.Array, temp.Offset + readPosition, targetDatas, offset + canReadCount - let, copySize);
                let -= copySize;
                readPosition += copySize;
            }
            return canReadCount;
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
        /// 得到字节段
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal ArraySegment<byte> GetItem(Int32 index)
        {
            if (index >= this.m_Datas.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            return this.m_Datas[index];
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
        //private int m_IsFree = 0;
        /// <summary>
        /// 清空包
        /// </summary>
        public void Clear()
        {
            //释放
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
            Clear();
        }
        #endregion
    }
}

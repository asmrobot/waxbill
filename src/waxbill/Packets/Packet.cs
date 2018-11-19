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
        private PacketBufferPool bufferPool;//缓存管理器  
        private List<ArraySegment<byte>> datasList;//内部数据存储              
        private int listIndex;//当前列表索引
        private int currentPosition;//内部数据存储最后一项的位置
        private bool isStart;
        private Int32 count;

        /// <summary>
        /// 是否开始
        /// </summary>
        internal bool IsStart
        {
            get
            {
                return isStart;
            }
            set
            {
                isStart = value;
            }
        }


        /// <summary>
        /// 总数据大小
        /// </summary>
        public Int64 Count
        {
            get
            {
                return this.count;
            }
        }
        
        
        public Packet(PacketBufferPool packetBufferPool)
        {
            this.count = 0;
            this.bufferPool = packetBufferPool;
            this.datasList = new List<ArraySegment<byte>>();
            this.listIndex = -1;
            this.currentPosition = 0;
        }
        

        /// <summary>
        /// todo:得到所有数据，慎用，会增大GC负担
        /// </summary>
        public byte[] Read()
        {
            byte[] datas = new byte[this.count];
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
            if (bufferOffset > this.count)
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
            int readArrayIndex = bufferOffset / this.bufferPool.BufferSize;//当前数组
            int readPosition = bufferOffset%this.bufferPool.BufferSize;//开始字节

            int canReadCount = Math.Min(count, this.count - bufferOffset);
            int let = canReadCount;
            ArraySegment<byte> temp;
            while (let > 0)
            {
                if (readPosition >= this.bufferPool.BufferSize)
                {
                    readArrayIndex++;
                    readPosition = 0;
                }
                int copySize = Math.Min(this.bufferPool.BufferSize - readPosition, let);
                temp = this.datasList[readArrayIndex];
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
                    idleCount = bufferPool.BufferSize - this.currentPosition;//当前缓存空闲
                    copySize = Math.Min(let, idleCount);
                    var temp = this.datasList[listIndex];
                    Marshal.Copy(bytes,  temp.Array, temp.Offset + this.currentPosition, copySize);
                    bytes += copySize;
                    this.currentPosition += copySize;
                    let -= copySize;
                    if (let <= 0)
                    {
                        break;
                    }
                    EnsureBuffer();
                }
                this.count += count;
            }
        }

        

        /// <summary>
        /// 添加字节段
        /// </summary>
        /// <param name="bytes"></param>
        public void Write(ArraySegment<byte> bytes)
        {
            if (bytes.Array == null || bytes.Count <= 0 || (bytes.Offset + bytes.Count) > bytes.Array.Length)
            {
                return;
            }
            EnsureBuffer();
            int idleCount = 0;
            int copySize = 0;
            int let = bytes.Count;//剩余copy字节数
            while (let > 0)
            {
                idleCount = bufferPool.BufferSize - this.currentPosition;//当前缓存空闲
                copySize = Math.Min(let, idleCount);
                var temp = this.datasList[listIndex];
                Buffer.BlockCopy(bytes.Array, bytes.Offset + bytes.Count - let, temp.Array, temp.Offset + this.currentPosition, copySize);
                this.currentPosition += copySize;
                let -= copySize;
                if (let <= 0)
                {
                    break;
                }
                EnsureBuffer();
            }
            this.count += bytes.Count;

            
        }

        private void EnsureBuffer()
        {
            if (this.listIndex < 0)
            {
                ArraySegment<byte> buffer;
                if (!this.bufferPool.TryGet(out buffer))
                {
                    throw new Exception("获取包缓存区失败");
                }
                this.datasList.Add(buffer);
                this.listIndex = 0;
                this.currentPosition = 0;
            }

            if (this.currentPosition >= this.bufferPool.BufferSize)
            {
                ArraySegment<byte> buffer;
                if (!this.bufferPool.TryGet(out buffer))
                {
                    throw new Exception("获取包缓存区失败");
                }
                this.datasList.Add(buffer);

                this.listIndex++;
                this.currentPosition = 0;
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
                if (index >= this.count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                int listoffset = index / this.bufferPool.BufferSize;
                int offset = index % this.bufferPool.BufferSize;
                var segment = this.datasList[listoffset];
                return segment.Array[segment.Offset + offset];
            }
            set
            {
                if (index >= this.count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                int listoffset = index / this.bufferPool.BufferSize;
                int offset = index % this.bufferPool.BufferSize;
                var segment = this.datasList[listoffset];
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
            for (int i = 0; i < this.datasList.Count; i++)
            {
                this.bufferPool.Release(this.datasList[i]);
            }

            this.datasList.Clear();
            
            this.count = 0;
            this.isStart = false;
            this.listIndex = -1;
            this.currentPosition = 0;
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

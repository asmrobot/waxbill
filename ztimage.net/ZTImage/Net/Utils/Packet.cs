namespace ZTImage.Net.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public class Packet : IDisposable
    {
        private BufferManager m_BufferManager;
        private int m_Count = 0;
        private int m_CurrentPosition;
        private List<ArraySegment<byte>> m_Datas;
        private int m_ForecastSize;
        private bool m_IsStart;
        private int m_ListIndex;

        internal Packet(BufferManager bufferManager)
        {
            this.m_BufferManager = bufferManager;
            this.m_Datas = new List<ArraySegment<byte>>();
            this.m_ListIndex = -1;
            this.m_CurrentPosition = 0;
        }

        public void Clear()
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
            this.Clear();
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

        internal ArraySegment<byte> GetItem(int index)
        {
            if (index >= this.m_Datas.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            return this.m_Datas[index];
        }

        public byte[] Read()
        {
            byte[] targetDatas = new byte[this.m_Count];
            this.Read(0, targetDatas, 0, targetDatas.Length);
            return targetDatas;
        }

        public int Read(int sourceOffset, byte[] targetDatas, int offset, int count)
        {
            if (sourceOffset > this.m_Count)
            {
                return 0;
            }
            if (targetDatas == null)
            {
                throw new ArgumentNullException("提供的空间为空");
            }
            if ((offset + count) > targetDatas.Length)
            {
                throw new ArgumentOutOfRangeException("没有提供足够大的空间，装填数据");
            }
            int num = sourceOffset / this.m_BufferManager.BufferSize;
            int num2 = sourceOffset % this.m_BufferManager.BufferSize;
            int num3 = Math.Min(count, this.m_Count - sourceOffset);
            int num4 = num3;
            while (num4 > 0)
            {
                if (num2 >= this.m_BufferManager.BufferSize)
                {
                    num++;
                    num2 = 0;
                }
                int num5 = Math.Min(this.m_BufferManager.BufferSize - num2, num4);
                ArraySegment<byte> segment = this.m_Datas[num];
                Buffer.BlockCopy(segment.Array, segment.Offset + num2, targetDatas, (offset + num3) - num4, num5);
                num4 -= num5;
                num2 += num5;
            }
            return num3;
        }

        internal void Write(ArraySegment<byte> bytes)
        {
            if ((bytes != new ArraySegment<byte>()) && (bytes.Count > 0))
            {
                this.EnsureBuffer();
                int num = 0;
                int count = 0;
                int num3 = bytes.Count;
                while (num3 > 0)
                {
                    num = this.m_BufferManager.BufferSize - this.m_CurrentPosition;
                    count = Math.Min(num3, num);
                    ArraySegment<byte> segment2 = this.m_Datas[this.m_ListIndex];
                    Buffer.BlockCopy(bytes.Array, (bytes.Offset + bytes.Count) - num3, segment2.Array, segment2.Offset + this.m_CurrentPosition, count);
                    this.m_CurrentPosition += count;
                    num3 -= count;
                    if (num3 <= 0)
                    {
                        break;
                    }
                    this.EnsureBuffer();
                }
                this.m_Count += bytes.Count;
            }
        }

        public int Count
        {
            get
            {
                return this.m_Count;
            }
        }

        internal int ForecastSize
        {
            get
            {
                return this.m_ForecastSize;
            }
            set
            {
                this.m_ForecastSize = value;
            }
        }

        internal bool IsStart
        {
            get
            {
                return this.m_IsStart;
            }
            set
            {
                this.m_IsStart = value;
            }
        }

        public byte this[int index]
        {
            get
            {
                if (index >= this.m_Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                int num = index / this.m_BufferManager.BufferSize;
                int num2 = index % this.m_BufferManager.BufferSize;
                ArraySegment<byte> segment = this.m_Datas[num];
                return segment.Array[segment.Offset + num2];
            }
            set
            {
                if (index >= this.m_Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                int num = index / this.m_BufferManager.BufferSize;
                int num2 = index % this.m_BufferManager.BufferSize;
                ArraySegment<byte> segment = this.m_Datas[num];
                segment.Array[segment.Offset + num2] = value;
            }
        }

        public int ItemCount
        {
            get
            {
                return this.m_Datas.Count;
            }
        }
    }
}


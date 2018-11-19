using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace waxbill.Pools
{
    public class SendingQueue : IList<ArraySegment<byte>>
    {
        public static readonly SendingQueue Null = default(SendingQueue);


        public static readonly ArraySegment<byte> Empty = default(ArraySegment<byte>);

        private bool isReadOnly;
        private ArraySegment<byte>[] global;//队列全局数组
        private readonly int globalOffset;//队列在全局中数组开始偏移
        private readonly int capacity;//队列容量

        private int offset;//在全局中数组开始偏移
        private int count;//数量
        
        private int updateing;

        public Int32 Capacity
        {
            get
            {
                return capacity;
            }
        }
        
        public SendingQueue(ArraySegment<byte>[] source, int offset, int capity)
        {
            this.global = source;
            this.offset=this.globalOffset = offset;
            this.capacity = capity;

            this.count = 0;
            
            isReadOnly = true;
        }

        public bool Enqueue(ArraySegment<byte> item)
        {
            if (!this.isReadOnly)
            {
                Interlocked.Increment(ref this.updateing);
                while (!this.isReadOnly)
                {
                    bool conflict = false;
                    if (this.TryEnqueue(item, out conflict))
                    {
                        Interlocked.Decrement(ref this.updateing);
                        return true;
                    }
                    if (!conflict)
                    {
                        break;
                    }
                }
                Interlocked.Decrement(ref this.updateing);
            }
            return false;
        }

        public bool Enqueue(IList<ArraySegment<byte>> items)
        {
            if (!this.isReadOnly)
            {
                Interlocked.Increment(ref this.updateing);
                while (!this.isReadOnly)
                {
                    bool conflict = false;
                    if (this.TryEnQueue(items, out conflict))
                    {
                        Interlocked.Decrement(ref this.updateing);
                        return true;
                    }
                    if (!conflict)
                    {
                        break;
                    }
                }
                Interlocked.Decrement(ref this.updateing);
            }
            return false;
        }
        
        public void StartQueue()
        {
            this.isReadOnly = false;
        }

        public void StopQueue()
        {
            if (!this.isReadOnly)
            {
                this.isReadOnly = true;
                if (this.updateing >= 0)
                {
                    SpinWait wait = new SpinWait();
                    wait.SpinOnce();
                    while (this.updateing > 0)
                    {
                        wait.SpinOnce();
                    }
                }
            }
        }

        /// <summary>
        /// 从队列的开始处剔除指定字节数
        /// </summary>
        /// <param name="byteCount"></param>
        public void TrimByte(int byteCount)
        {
            int num = this.count - this.offset;
            int num2 = 0;
            for (int i = this.offset; i < num; i++)
            {
                ArraySegment<byte> segment = this.global[this.globalOffset + i];
                num2 += segment.Count;
                if (num2 > byteCount)
                {
                    this.offset = i;
                    int count = num2 - byteCount;
                    this.global[this.globalOffset + i] = new ArraySegment<byte>(segment.Array, (segment.Offset + segment.Count) - count, count);
                    return;
                }
            }
        }

        
        private bool TryEnqueue(ArraySegment<byte> item, out bool conflict)
        {
            conflict = false;
            if (isReadOnly)
            {
                return false;
            }
            int currentCount = this.count;
            if (currentCount >= this.capacity)
            {
                return false;
            }
            if (Interlocked.CompareExchange(ref this.count, currentCount + 1, currentCount) != currentCount)
            {
                conflict = true;
                return false;
            }
            this.global[this.offset + currentCount] = item;
            return true;
        }

        private bool TryEnQueue(IList<ArraySegment<byte>> items, out bool conflict)
        {
            conflict = false;
            if (isReadOnly)
            {
                return false;
            }
            int currentCount = this.count;
            if ((currentCount + items.Count) >= this.capacity)
            {
                return false;
            }
            if (Interlocked.CompareExchange(ref this.count, currentCount + items.Count, currentCount) != currentCount)
            {
                conflict = true;
                return false;
            }
            for (int i = 0; i < items.Count; i++)
            {
                this.global[(this.offset + currentCount) + i] = items[i];
            }
            return true;
        }



        #region IList

        public void Add(ArraySegment<byte> item)
        {
            throw new NotSupportedException();
        }


        //todo
        public void Clear()
        {
            for (int i = 0; i < this.count; i++)
            {
                this.global[this.offset + i] = Empty;
            }
            this.count = 0;
        }

        public bool Contains(ArraySegment<byte> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(ArraySegment<byte>[] array, int arrayIndex)
        {
            for (int i = 0; i < this.Count; i++)
            {
                array[arrayIndex + i] = this[i];
            }
        }
        
        public IEnumerator<ArraySegment<byte>> GetEnumerator()
        {
            for (int i = 0; i < count; i++)
            {
                yield return this.global[offset + i];
            }
        }

        public int IndexOf(ArraySegment<byte> item)
        {
            throw new NotSupportedException();
        }

        public void Insert(int index, ArraySegment<byte> item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(ArraySegment<byte> item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            get
            {
                return this.count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return this.isReadOnly;
            }
        }

        public ArraySegment<byte> this[int index]
        {
            get
            {
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException("index 小于0");
                }
                return this.global[this.offset + index];
            }
            set
            {
                throw new NotSupportedException();
            }
        }
        #endregion


    }
}


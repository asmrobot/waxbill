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
    

    public class SendingQueue : IList<ArraySegment<byte>>, ICollection<ArraySegment<byte>>, IEnumerable<ArraySegment<byte>>, IEnumerable
    {
        public static readonly ArraySegment<byte> m_NULL;

        private int capity;
        private int currentCount;
        private ArraySegment<byte>[] global;
        private int innerOffset;
        private bool isReadOnly;
        
        private int offset;
        private int updateing;

        public SendingQueue(ArraySegment<byte>[] source, int offset, int capity)
        {
            this.global = source;
            this.offset = offset;
            this.capity = capity;
            this.innerOffset = 0;
        }

        public void Add(ArraySegment<byte> item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            for (int i = 0; i < this.currentCount; i++)
            {
                this.global[this.offset + i] = m_NULL;
            }
            this.currentCount = 0;
            this.innerOffset = 0;
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

        public bool EnQueue(ArraySegment<byte> item)
        {
            if (!this.isReadOnly)
            {
                Interlocked.Increment(ref this.updateing);
                while (!this.isReadOnly)
                {
                    bool conflict = false;
                    if (this.TryEnQueue(item, out conflict))
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

        public bool EnQueue(IList<ArraySegment<byte>> items)
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

        
        public IEnumerator<ArraySegment<byte>> GetEnumerator()
        {
            for (int i = 0; i < currentCount; i++)
            {
                yield return this.global[offset + currentCount];
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void TrimByte(int byteCount)
        {
            int num = this.currentCount - this.innerOffset;
            int num2 = 0;
            for (int i = this.innerOffset; i < num; i++)
            {
                ArraySegment<byte> segment = this.global[this.offset + i];
                num2 += segment.Count;
                if (num2 > byteCount)
                {
                    this.innerOffset = i;
                    int count = num2 - byteCount;
                    this.global[this.offset + i] = new ArraySegment<byte>(segment.Array, (segment.Offset + segment.Count) - count, count);
                    return;
                }
            }
        }

        private bool TryEnQueue(ArraySegment<byte> item, out bool conflict)
        {
            conflict = false;
            int currentCount = this.currentCount;
            if (currentCount >= this.capity)
            {
                return false;
            }
            if (Interlocked.CompareExchange(ref this.currentCount, currentCount + 1, currentCount) != currentCount)
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
            int currentCount = this.currentCount;
            if ((currentCount + items.Count) >= this.capity)
            {
                return false;
            }
            if (Interlocked.CompareExchange(ref this.currentCount, currentCount + items.Count, currentCount) != currentCount)
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

        public int Count
        {
            get
            {
                return (this.currentCount - this.innerOffset);
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
                int num = (this.offset + this.innerOffset) + index;
                return this.global[num];
            }
            set
            {
                throw new NotSupportedException();
            }
        }
        
    }
}


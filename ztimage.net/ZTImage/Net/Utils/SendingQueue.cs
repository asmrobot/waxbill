namespace ZTImage.Net.Utils
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;

    public class SendingQueue : IList<ArraySegment<byte>>, ICollection<ArraySegment<byte>>, IEnumerable<ArraySegment<byte>>, IEnumerable
    {
        private int m_Capity;
        private int m_CurrentCount;
        private ArraySegment<byte>[] m_Global;
        private int m_InnerOffset;
        private bool m_IsReadOnly;
        private static readonly ArraySegment<byte> m_NULL;
        private int m_Offset;
        private int m_Updateing;

        public SendingQueue(ArraySegment<byte>[] source, int offset, int capity)
        {
            this.m_Global = source;
            this.m_Offset = offset;
            this.m_Capity = capity;
            this.m_InnerOffset = 0;
        }

        public void Add(ArraySegment<byte> item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            for (int i = 0; i < this.m_CurrentCount; i++)
            {
                this.m_Global[this.m_Offset + i] = m_NULL;
            }
            this.m_CurrentCount = 0;
            this.m_InnerOffset = 0;
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
            if (!this.m_IsReadOnly)
            {
                Interlocked.Increment(ref this.m_Updateing);
                while (!this.m_IsReadOnly)
                {
                    bool conflict = false;
                    if (this.TryEnQueue(item, out conflict))
                    {
                        Interlocked.Decrement(ref this.m_Updateing);
                        return true;
                    }
                    if (!conflict)
                    {
                        break;
                    }
                }
                Interlocked.Decrement(ref this.m_Updateing);
            }
            return false;
        }

        public bool EnQueue(IList<ArraySegment<byte>> items)
        {
            if (!this.m_IsReadOnly)
            {
                Interlocked.Increment(ref this.m_Updateing);
                while (!this.m_IsReadOnly)
                {
                    bool conflict = false;
                    if (this.TryEnQueue(items, out conflict))
                    {
                        Interlocked.Decrement(ref this.m_Updateing);
                        return true;
                    }
                    if (!conflict)
                    {
                        break;
                    }
                }
                Interlocked.Decrement(ref this.m_Updateing);
            }
            return false;
        }

        [IteratorStateMachine(typeof(<GetEnumerator>d__31))]
        public IEnumerator<ArraySegment<byte>> GetEnumerator()
        {
            return new <GetEnumerator>d__31(0) { <>4__this = this };
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
            this.m_IsReadOnly = false;
        }

        public void StopQueue()
        {
            if (!this.m_IsReadOnly)
            {
                this.m_IsReadOnly = true;
                if (this.m_Updateing >= 0)
                {
                    SpinWait wait = new SpinWait();
                    wait.SpinOnce();
                    while (this.m_Updateing > 0)
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
            int num = this.m_CurrentCount - this.m_InnerOffset;
            int num2 = 0;
            for (int i = this.m_InnerOffset; i < num; i++)
            {
                ArraySegment<byte> segment = this.m_Global[this.m_Offset + i];
                num2 += segment.Count;
                if (num2 > byteCount)
                {
                    this.m_InnerOffset = i;
                    int count = num2 - byteCount;
                    this.m_Global[this.m_Offset + i] = new ArraySegment<byte>(segment.Array, (segment.Offset + segment.Count) - count, count);
                    return;
                }
            }
        }

        private bool TryEnQueue(ArraySegment<byte> item, out bool conflict)
        {
            conflict = false;
            int currentCount = this.m_CurrentCount;
            if (currentCount >= this.m_Capity)
            {
                return false;
            }
            if (Interlocked.CompareExchange(ref this.m_CurrentCount, currentCount + 1, currentCount) != currentCount)
            {
                conflict = true;
                return false;
            }
            this.m_Global[this.m_Offset + currentCount] = item;
            return true;
        }

        private bool TryEnQueue(IList<ArraySegment<byte>> items, out bool conflict)
        {
            conflict = false;
            int currentCount = this.m_CurrentCount;
            if ((currentCount + items.Count) >= this.m_Capity)
            {
                return false;
            }
            if (Interlocked.CompareExchange(ref this.m_CurrentCount, currentCount + items.Count, currentCount) != currentCount)
            {
                conflict = true;
                return false;
            }
            for (int i = 0; i < items.Count; i++)
            {
                this.m_Global[(this.m_Offset + currentCount) + i] = items[i];
            }
            return true;
        }

        public int Count
        {
            get
            {
                return (this.m_CurrentCount - this.m_InnerOffset);
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return this.m_IsReadOnly;
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
                int num = (this.m_Offset + this.m_InnerOffset) + index;
                return this.m_Global[num];
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        [CompilerGenerated]
        private sealed class <GetEnumerator>d__31 : IEnumerator<ArraySegment<byte>>, IDisposable, IEnumerator
        {
            private int <>1__state;
            private ArraySegment<byte> <>2__current;
            public SendingQueue <>4__this;
            private int <i>5__1;

            [DebuggerHidden]
            public <GetEnumerator>d__31(int <>1__state)
            {
                this.<>1__state = <>1__state;
            }

            private bool MoveNext()
            {
                int num = this.<>1__state;
                SendingQueue queue = this.<>4__this;
                if (num == 0)
                {
                    this.<>1__state = -1;
                    this.<i>5__1 = 0;
                    while (this.<i>5__1 < queue.Count)
                    {
                        this.<>2__current = queue.m_Global[(queue.m_Offset + queue.m_InnerOffset) + this.<i>5__1];
                        this.<>1__state = 1;
                        return true;
                    Label_0055:
                        this.<>1__state = -1;
                        int num2 = this.<i>5__1;
                        this.<i>5__1 = num2 + 1;
                    }
                    return false;
                }
                if (num != 1)
                {
                    return false;
                }
                goto Label_0055;
            }

            [DebuggerHidden]
            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            [DebuggerHidden]
            void IDisposable.Dispose()
            {
            }

            ArraySegment<byte> IEnumerator<ArraySegment<byte>>.Current
            {
                [DebuggerHidden]
                get
                {
                    return this.<>2__current;
                }
            }

            object IEnumerator.Current
            {
                [DebuggerHidden]
                get
                {
                    return this.<>2__current;
                }
            }
        }
    }
}


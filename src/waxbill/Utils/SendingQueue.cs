using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Threading;

namespace waxbill.Utils
{
    /// <summary>
    /// 发送队列
    /// </summary>
    public class SendingQueue:IList<ArraySegment<byte>>
    {
        private static readonly ArraySegment<byte> m_NULL = default(ArraySegment<byte>);
        private ArraySegment<byte>[] m_Global;
        private int m_Offset;
        private int m_Capity;
        private int m_InnerOffset;

        /// <summary>
        /// 默认位置
        /// </summary>
        private int m_CurrentCount;
        private bool m_IsReadOnly=false;
        private int m_Updateing = 0;//进入队列数


        public SendingQueue(ArraySegment<byte>[] source,int offset,int capity)
        {
            this.m_Global = source;
            this.m_Offset = offset;
            this.m_Capity = capity;
            this.m_InnerOffset = 0;
        }
        
        public void StartQueue()
        {
            m_IsReadOnly = false;
        }

        public void StopQueue()
        {
            if (m_IsReadOnly)
            {
                return;
            }

            m_IsReadOnly = true;
            if (m_Updateing < 0)
            {
                return;
            }
            
            SpinWait wait = new SpinWait();
            wait.SpinOnce();
            while (m_Updateing > 0)
            {
                wait.SpinOnce();
            }            
        }
        
        public bool EnQueue(ArraySegment<byte> item)
        {
            if (m_IsReadOnly)
            {
                return false;
            }

            Interlocked.Increment(ref m_Updateing);
            while (!m_IsReadOnly)
            {
                bool conflict = false;
                if (TryEnQueue(item, out conflict))
                {
                    Interlocked.Decrement(ref m_Updateing);
                    return true;
                }

                if (!conflict)
                {
                    break;
                }
            }
            Interlocked.Decrement(ref m_Updateing);
            return false;
        }

        private bool TryEnQueue(ArraySegment<byte> item, out bool conflict)
        {
            conflict = false;
            int currentCount = m_CurrentCount;
            if (currentCount >= m_Capity)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref m_CurrentCount, currentCount + 1, currentCount) != currentCount)
            {
                conflict = true;
                return false;
            }

            //添加
            this.m_Global[this.m_Offset + currentCount] = item;
            return true;
        }

        public bool EnQueue(IList<ArraySegment<byte>> items)
        {
            if (m_IsReadOnly)
            {
                return false;
            }

            Interlocked.Increment(ref m_Updateing);
            while (!m_IsReadOnly)
            {
                bool conflict = false;
                if (TryEnQueue(items, out conflict))
                {
                    Interlocked.Decrement(ref m_Updateing);
                    return true;
                }

                if (!conflict)
                {
                    break;
                }
            }
            Interlocked.Decrement(ref m_Updateing);
            return false;
        }

        private bool TryEnQueue(IList<ArraySegment<byte>> items, out bool conflict)
        {
            conflict = false;            
            int oldCurrent = m_CurrentCount;
            if (oldCurrent+items.Count >= m_Capity)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref m_CurrentCount, oldCurrent + items.Count, oldCurrent) != oldCurrent)
            {
                conflict = true;
                return false;
            }
            
            for (int i = 0; i < items.Count; i++)
            {
                this.m_Global[this.m_Offset + oldCurrent+i] = items[i];
            }
            return true;
        }

        /// <summary>
        /// 过滤指定数据量
        /// </summary>
        /// <param name="byteCount"></param>
        public void TrimByte(int byteCount)
        {
            var innerCount = m_CurrentCount - m_InnerOffset;
            var subTotal = 0;

            for (var i = m_InnerOffset; i < innerCount; i++)
            {
                var segment = m_Global[m_Offset + i];
                subTotal += segment.Count;

                if (subTotal <= byteCount)
                    continue;

                m_InnerOffset = i;

                var rest = subTotal - byteCount;
                m_Global[m_Offset + i] = new ArraySegment<byte>(segment.Array, segment.Offset + segment.Count - rest, rest);

                break;
            }
        }
        
        #region IList

        public ArraySegment<byte> this[int index]
        {
            get
            {
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException("index 小于0");
                }
                var targetIndex = m_Offset + m_InnerOffset + index;
                var value = this.m_Global[targetIndex];
                return value;
            }
            set
            {
                throw new NotSupportedException();
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

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get
            {
                return this.m_CurrentCount-this.m_InnerOffset;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return this.m_IsReadOnly;
            }
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
            m_CurrentCount = 0;
            m_InnerOffset = 0;
        }

        public bool Contains(ArraySegment<byte> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(ArraySegment<byte>[] array, int arrayIndex)
        {
            for (int i = 0; i < Count; i++)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        public bool Remove(ArraySegment<byte> item)
        {
            throw new NotImplementedException();
        }


        public IEnumerator<ArraySegment<byte>> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this.m_Global[this.m_Offset+this.m_InnerOffset + i];
            }
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion
    }
}

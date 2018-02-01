using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using waxbill.Utils;
using ZTImage.Log;

namespace waxbill.Libuv
{
    public unsafe class UVWriteRequest : UVRequest, IList<UVIntrop.uv_buf_t>
    {
        public const Int32 QUEUE_SIZE = 6;
        
        private Action<UVWriteRequest, Int32, UVException, object> mWriteCallback;
        private object mWriteCallbackState;
        private GCHandle[] mPins;

        private bool m_IsReadOnly = false;
        private int m_Updateing = 0;//进入队列数
        internal UVIntrop.uv_buf_t* Buffer;
        internal int Offset;
        

        public UVWriteRequest() 
        {
            Int32 requestSize = UVIntrop.req_size(UVRequestType.WRITE);
            var bufferSize = Marshal.SizeOf(typeof(UVIntrop.uv_buf_t)) * QUEUE_SIZE;
            this.mPins = new GCHandle[QUEUE_SIZE];
            CreateMemory(requestSize + bufferSize);
            this.Buffer = (UVIntrop.uv_buf_t*)(this.handle + requestSize);
        }
        
        public void StartEnqueue()
        {
            m_IsReadOnly = false;
        }

        public void StopEnqueue()
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
        
        public bool Enqueue(ArraySegment<byte> item)
        {
            if (m_IsReadOnly)
            {
                return false;
            }

            Interlocked.Increment(ref m_Updateing);
            while (!m_IsReadOnly)
            {
                bool conflict = false;
                if (TryEnqueue(item, out conflict))
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

        private unsafe bool TryEnqueue(ArraySegment<byte> item, out bool conflict)
        {
            conflict = false;
            int currentCount = Offset;
            if (currentCount >= QUEUE_SIZE)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref Offset, currentCount + 1, currentCount) != currentCount)
            {
                conflict = true;
                return false;
            }

            //添加
            var gcHandle = GCHandle.Alloc(item.Array, GCHandleType.Pinned);
            mPins[currentCount]=gcHandle;
            this.Buffer[currentCount] = UVIntrop.buf_init(gcHandle.AddrOfPinnedObject() + item.Offset, item.Count);

            return true;
        }

        public bool Enqueue(IList<ArraySegment<byte>> items)
        {
            if (m_IsReadOnly)
            {
                return false;
            }

            Interlocked.Increment(ref m_Updateing);
            while (!m_IsReadOnly)
            {
                bool conflict = false;
                if (TryEnqueue(items, out conflict))
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

        private bool TryEnqueue(IList<ArraySegment<byte>> items, out bool conflict)
        {
            conflict = false;
            int oldCurrent = Offset;
            if (oldCurrent + items.Count >= QUEUE_SIZE)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref Offset, oldCurrent + items.Count, oldCurrent) != oldCurrent)
            {
                conflict = true;
                return false;
            }

            for (int i = 0; i < items.Count; i++)
            {
                ArraySegment<byte> item = items[i];
                var gcHandle = GCHandle.Alloc(item.Array, GCHandleType.Pinned);
                mPins[oldCurrent+i] = gcHandle;
                this.Buffer[oldCurrent+i] = UVIntrop.buf_init(gcHandle.AddrOfPinnedObject() + item.Offset, item.Count);
            }
            return true;
        }

        public void SetCallback(Action<UVWriteRequest, Int32, UVException, object> callback, object state)
        {
            this.mWriteCallback = callback;
            this.mWriteCallbackState = state;
        }

        public void RaiseSended(Int32 status,UVException error)
        {
            try
            {
                if (this.mWriteCallback != null)
                {
                    this.mWriteCallback(this, status, error, this.mWriteCallbackState);
                }
            }
            catch (Exception ex)
            {
                this.mWriteCallback = null;
                this.mWriteCallbackState = null;
                UnpinGCHandles();
                Trace.Error("UvWriteCb", ex);
                throw;
            }
        }
        
        // Safe handle has instance method called Unpin
        // so using UnpinGcHandles to avoid conflict
        internal void UnpinGCHandles()
        {            
            for (var i = 0; i < this.Offset; i++)
            {
                this.mPins[i].Free();
                this.mPins[i] = default(GCHandle);
            }
        }
        
        #region IList

        public UVIntrop.uv_buf_t this[int index]
        {
            get
            {
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException("index 小于0");
                }
                
                var value = this.Buffer[index];
                return value;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public int IndexOf(UVIntrop.uv_buf_t item)
        {
            throw new NotSupportedException();
        }

        public void Insert(int index, UVIntrop.uv_buf_t item)
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
                return this.Offset;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return this.m_IsReadOnly;
            }
        }


        public void Add(UVIntrop.uv_buf_t item)
        {
            throw new NotSupportedException();
        }



        public void Clear()
        {
            this.UnpinGCHandles();
            this.mWriteCallback = null;
            this.mWriteCallbackState = null;

            this.Offset = 0;

        }

        public bool Contains(UVIntrop.uv_buf_t item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(UVIntrop.uv_buf_t[] array, int arrayIndex)
        {
            for (int i = 0; i < Count; i++)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        public bool Remove(UVIntrop.uv_buf_t item)
        {
            throw new NotImplementedException();
        }


        public unsafe IEnumerator<UVIntrop.uv_buf_t> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
        #endregion


    }
}

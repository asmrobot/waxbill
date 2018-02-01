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
    public unsafe class UVRequest : UVMemory
    {
        

        public UVRequest() : base(GCHandleType.Normal)
        {}


        protected void CreateRequest(Int32 size)
        {
            CreateMemory(size);
        }

        protected void CreateRequest(UVRequestType type)
        {
            CreateMemory(UVIntrop.req_size(type));
        }
        
        protected override bool ReleaseHandle()
        {
            IntPtr memory = handle;
            if (memory != IntPtr.Zero)
            {
                DestroyMemory(handle);
                handle = IntPtr.Zero;
            }

            return true;
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
                
                var value = this.mBufs[index];
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
                return this.mCurrentQueueSize;
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
            this.mCallback = null;
            this.mState = null;

            this.mCurrentQueueSize = 0;

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

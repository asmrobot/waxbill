using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using waxbill.Exceptions;

namespace waxbill.Libuv
{

    public class UVStreamHandle : UVHandle
    {
        public delegate UVIntrop.uv_buf_t AllocCallback(UVStreamHandle handle, Int32 suggestedSize,object allocState);
        public delegate void ReadCallback(UVStreamHandle handle, Int32 nread, UVException exception,ref UVIntrop.uv_buf_t buf, object readState);


        private readonly static UVIntrop.uv_connection_cb mOnConnection = UVConnectionCb;
        private readonly static UVIntrop.uv_read_cb mOnRead = UVReadCb;
        private readonly static UVIntrop.uv_alloc_cb mOnAlloc = UVAllocCb;

        private GCHandle mStreamGCHandle;
        private Action<UVStreamHandle, Int32, UVException, Object> mConnectionCallback;
        private object mConnectionCallbackState;

        private AllocCallback mAllocCallback;
        private object mallocCallbackState;
        private ReadCallback mReadCallback;
        private object mReadCallbackState;
        

        public void Listen(int backlog,Action<UVStreamHandle,Int32 ,UVException,Object> callback,object state)
        {
            if (mStreamGCHandle.IsAllocated)
            {
                throw new CanotRepeatException("只能监听一次端口 ");
            }
            try
            {
                mConnectionCallbackState = state;
                mConnectionCallback = callback;
                mStreamGCHandle = GCHandle.Alloc(this, GCHandleType.Normal);
                UVIntrop.listen(this, backlog, mOnConnection);
            }
            catch
            {
                mConnectionCallback = null;
                mConnectionCallbackState = null;
                if (mStreamGCHandle.IsAllocated)
                {
                    mStreamGCHandle.Free();
                }
            }
            
        }

        public void Accept(UVStreamHandle handle)
        {
            UVIntrop.accept(this, handle);
        }

        public void ReadStart(AllocCallback allocCallback, ReadCallback readCallback,object allocState,object readState)
        {
            this.mAllocCallback = allocCallback;
            this.mReadCallback = readCallback;
            this.mReadCallbackState = readState;
            this.mallocCallbackState = allocState;
            UVIntrop.read_start(this, mOnAlloc, mOnRead);
        }

        public void ReadStop()
        {
            UVIntrop.read_stop(this);
        } 

        public void Write(byte[] datas)
        {
            Write(datas,0, datas.Length);
        }

        unsafe public void Write(byte[] datas,Int32 offset, Int32 count)
        {
            fixed (byte* p = datas)
            {
                UVIntrop.uv_buf_t[] mbuf = new UVIntrop.uv_buf_t[]{
                    UVIntrop.buf_init((IntPtr)(p+offset), count)
                };
                
                UVIntrop.try_write(this, mbuf, 1);
            }
        }
        
        protected override bool ReleaseHandle()
        {
            this.mConnectionCallbackState = null;
            this.mConnectionCallback = null;
            
            if (this.mStreamGCHandle.IsAllocated)
            {
                this.mStreamGCHandle.Free();
            }
            return base.ReleaseHandle();
        }

        #region cb
        private static void UVConnectionCb(IntPtr server, Int32 status)
        {
            UVException error;
            UVIntrop.Check(status, out error);
            UVStreamHandle stream = UVMemory.FromIntPtr<UVStreamHandle>(server);
            try
            {
                stream.mConnectionCallback(stream, status, error, stream.mConnectionCallbackState);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
       
        private static void UVAllocCb(IntPtr handle, int suggestedSize, out UVIntrop.uv_buf_t buf)
        {
            UVStreamHandle target=FromIntPtr<UVStreamHandle>(handle);
            if (target == null)
            {
                //buf = UVIntrop.buf_init(IntPtr.Zero, 0);
                throw new WaxbillException("流已释放");
            }
            try
            {
                buf = target.mAllocCallback(target, suggestedSize,target.mallocCallbackState);
            }
            //catch (Exception ex)
            catch
            {
                //todo:清理操作
                throw new WaxbillException("分配内存出错");
            }
        }

        unsafe private static void UVReadCb(IntPtr handle, int nread, ref UVIntrop.uv_buf_t buf)
        {
            UVException ex;
            UVIntrop.Check(nread, out ex);

            UVStreamHandle target = FromIntPtr<UVStreamHandle>(handle);
            if (target == null)
            {
                throw new WaxbillException("流已释放");
            }
            target.mReadCallback(target, nread, ex,ref buf, target.mReadCallbackState);
        }
        #endregion
    }
}

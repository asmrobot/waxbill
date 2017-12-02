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

        private readonly static UVIntrop.uv_connection_cb mOnConnection = UVConnectionCb;
        private readonly static UVIntrop.uv_read_cb mOnRead = UVReadCb;
        private readonly static UVIntrop.uv_alloc_cb mOnAlloc = UVAllocCb;

        private GCHandle mStreamGCHandle;
        private Action<UVStreamHandle, Int32, UVException, Object> mConnectionCallback;
        private object mCallbackState;

        public void Listen(int backlog,Action<UVStreamHandle,Int32 ,UVException,Object> callback,object state)
        {
            if (mStreamGCHandle.IsAllocated)
            {
                throw new CanotRepeatException("只能监听一次端口 ");
            }
            try
            {
                mCallbackState = state;
                mConnectionCallback = callback;
                mStreamGCHandle = GCHandle.Alloc(this, GCHandleType.Normal);
                UVIntrop.listen(this, backlog, mOnConnection);
            }
            catch
            {
                mConnectionCallback = null;
                mCallbackState = null;
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

        public void ReadStart()
        {
            UVIntrop.read_start(this, mOnAlloc, mOnRead);
        }

        public void ReadStop()
        {
            UVIntrop.read_stop(this);
        }


        public void Write()
        {
            
        }

        public void TryWrite()
        {

        }



        protected override bool ReleaseHandle()
        {
            this.mCallbackState = null;
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
            UVIntrop.Check(status, out var error);

            UVStreamHandle stream = UVMemory.FromIntPtr<UVStreamHandle>(server);
            try
            {
                stream.mConnectionCallback(stream, status, error, stream.mCallbackState);
            }
            catch (Exception ex)
            {
                throw;
            }
            
        }
        private IntPtr Content=IntPtr.Zero;
        private Int32 Size = 0;

        private static void UVAllocCb(IntPtr handle, int suggestedSize, out UVIntrop.uv_buf_t buf)
        {
            UVStreamHandle target=FromIntPtr<UVStreamHandle>(handle);
            if (target.Content == IntPtr.Zero)
            {
                target.Size = suggestedSize;
                target.Content = Marshal.AllocHGlobal(suggestedSize);
                buf = UVIntrop.buf_init(target.Content, target.Size);
            }
            buf= new UVIntrop.uv_buf_t(target.Content, target.Size, UVIntrop.IsWindows);
        }

        unsafe private static void UVReadCb(IntPtr handle, int nread, ref UVIntrop.uv_buf_t buf)
        {
            UVException ex;
            UVIntrop.Check(nread, out ex);
            if (ex != null)
            {
                if (nread == UVIntrop.UV_EOF)
                {
                    Console.WriteLine("关闭鸟");
                    return;
                }
                Console.WriteLine("有错误");
                return;
            }

            if (nread == 0)
            {
                Console.WriteLine("据说可以忽略");
                return;
            }
            else
            {
                //read
                UVStreamHandle target = FromIntPtr<UVStreamHandle>(handle);
                byte[] t = new byte[nread];
                Marshal.Copy(target.Content, t, 0, nread);

                Console.WriteLine("读取字节数:" + nread.ToString()+","+System.Text.Encoding.UTF8.GetString(t));

                fixed (byte* p = t)
                {
                    IntPtr pointer = (IntPtr)p;
                    UVIntrop.uv_buf_t[] mbuf = new UVIntrop.uv_buf_t[]{
                        UVIntrop.buf_init(pointer, nread)
                    };


                    UVIntrop.uv_try_write(target, mbuf, 1);
                }
                
            }
        }
        #endregion
    }
}

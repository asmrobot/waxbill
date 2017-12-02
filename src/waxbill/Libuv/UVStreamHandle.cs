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
        private object mConnectionCallbackState;

        private Func<UVStreamHandle, Int32, UVIntrop.uv_buf_t> mAllocCallback;
        private Action<UVStreamHandle, Int32, UVException, UVIntrop.uv_buf_t, Object> mReadCallback;
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

        public void ReadStart(Func<UVStreamHandle, Int32, UVIntrop.uv_buf_t> allocCallback, Action<UVStreamHandle,Int32,UVException, UVIntrop.uv_buf_t, Object> readCallback,object readState)
        {
            //todo: 禁止重复调用
            this.mReadCallback = readCallback;
            this.mReadCallbackState = readState;
            UVIntrop.read_start(this, mOnAlloc, mOnRead);
        }

        public void ReadStop()
        {
            UVIntrop.read_stop(this);
        }
        
        public void TryWrite(byte[] datas)
        {
            TryWrite(datas, datas.Length);
        }

        unsafe public void TryWrite(byte[] datas, Int32 count)
        {
            fixed (byte* p = datas)
            {
                //TODO:mbuf fixed
                UVIntrop.uv_buf_t[] mbuf = new UVIntrop.uv_buf_t[]{
                    UVIntrop.buf_init((IntPtr)p, count)
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
            UVIntrop.Check(status, out var error);

            UVStreamHandle stream = UVMemory.FromIntPtr<UVStreamHandle>(server);
            try
            {
                stream.mConnectionCallback(stream, status, error, stream.mConnectionCallbackState);
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
            if (target == null)
            {
                //buf = UVIntrop.buf_init(IntPtr.Zero, 0);
                throw new WaxbillException("流已释放");
            }
            try
            {
                buf = target.mAllocCallback(target, suggestedSize);
            }
            catch (Exception ex)
            {
                //todo:清理操作
                throw new WaxbillException("分配内存出错");
            }
                
            
            //if (target.Content == IntPtr.Zero)
            //{
            //    target.Size = suggestedSize;
            //    target.Content = Marshal.AllocHGlobal(suggestedSize);
            //    buf = UVIntrop.buf_init(target.Content, target.Size);
            //}
            //buf= new UVIntrop.uv_buf_t(target.Content, target.Size, UVIntrop.IsWindows);
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
            target.mReadCallback(target, nread, ex,buf, target.mReadCallbackState);
            //if (ex != null)
            //{
            //    if (nread == UVIntrop.UV_EOF)
            //    {
            //        Console.WriteLine("关闭鸟");
            //        return;
            //    }
            //    Console.WriteLine("有错误");
            //    return;
            //}

            //if (nread == 0)
            //{
            //    Console.WriteLine("据说可以忽略");
            //    return;
            //}
            //else
            //{
            //    //read
                
            //    byte[] t = new byte[nread];
            //    Marshal.Copy(target.Content, t, 0, nread);

            //    Console.WriteLine("读取字节数:" + nread.ToString()+","+System.Text.Encoding.UTF8.GetString(t));

            //    target.TryWrite(t);
            //}
        }
        #endregion
    }
}

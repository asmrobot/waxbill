using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace waxbill.Libuv
{
    public class UVLoopHandle : UVMemory
    {
        public static readonly UVLoopHandle Define = new UVLoopHandle();

        public UVLoopHandle()
        {
            
            CreateMemory(UVIntrop.loop_size());
            UVIntrop.loop_init(this);
        }

        public void Start()
        {
            UVIntrop.run(this, (Int32)UVIntrop.UV_RUN_MODE.UV_RUN_DEFAULT);
        }

        public void AsyncStart()
        {
            Thread thread = new Thread(StartThread);
            thread.IsBackground = true;
            thread.Start();
        }

        private void StartThread(object state)
        {
            UVIntrop.run(this, (Int32)UVIntrop.UV_RUN_MODE.UV_RUN_DEFAULT);
        }

        public void Stop()
        {
            UVIntrop.stop(this);
        }
        

        protected unsafe override bool ReleaseHandle()
        {
            if (handle != IntPtr.Zero)
            {
                UVIntrop.uv_loop_close(this.handle);
                DestroyMemory(handle);
                handle = IntPtr.Zero;
            }

            return true;
        }
    }
}

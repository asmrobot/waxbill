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

        public void LoopClose()
        {
            UVIntrop.loop_close(this);
        }

        protected unsafe override bool ReleaseHandle()
        {
            if (handle != IntPtr.Zero)
            {
                //UVIntrop.loop_close(this);
                DestroyMemory(handle);
                handle = IntPtr.Zero;
            }

            return true;
        }
    }
}

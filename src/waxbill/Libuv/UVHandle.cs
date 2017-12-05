using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace waxbill.Libuv
{
    public abstract class UVHandle:UVMemory
    {
        private static readonly UVIntrop.uv_close_cb mDestroyMemory = DestroyMemory;

        public void CreateHandle(Int32 size)
        {
            CreateMemory(size);
        }

        public void CreateHandle(UVIntrop.HandleType type)
        {
            CreateMemory(UVIntrop.handle_size(type));
        }

        protected override bool ReleaseHandle()
        {
            IntPtr memory = handle;
            if (memory != IntPtr.Zero)
            {
                UVIntrop.close(handle, mDestroyMemory);
                handle = IntPtr.Zero;
            }
            
            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace waxbill.Libuv
{
    public unsafe abstract class UVMemory : SafeHandle
    {
        private GCHandleType mHandleType;
        public UVMemory(GCHandleType type=GCHandleType.Weak):base(IntPtr.Zero,true)
        {
            this.mHandleType = type;
        }
        
        public void CreateMemory(Int32 size)
        {
            handle = Marshal.AllocHGlobal(size);
            //handle = Marshal.AllocCoTaskMem(size);
            GCHandle gcHandlePtr = GCHandle.Alloc(this, this.mHandleType);
            *(IntPtr*)handle = GCHandle.ToIntPtr(gcHandlePtr);
        }

        public static void DestroyMemory(IntPtr memory)
        {
            IntPtr gcHandlePtr = *(IntPtr*)memory;
            DestroyMemory(memory, gcHandlePtr);
        }

        public static void DestroyMemory(IntPtr memory, IntPtr gcHandlePtr)
        {
            if (gcHandlePtr != IntPtr.Zero)
            {
                GCHandle gcHandle=GCHandle.FromIntPtr(gcHandlePtr);
                gcHandle.Free();
            }
            Marshal.FreeHGlobal(memory);
        }


        public override bool IsInvalid
        {
            get
            {
                return this.handle == IntPtr.Zero;
            }
        }
        
        public void Validate(Boolean closed = false)
        {

        }

        public IntPtr InternalGetHandle()
        {
            return handle;
        }


        public static T FromIntPtr<T>(IntPtr memory)
        {
            GCHandle gcHandle=GCHandle.FromIntPtr(*(IntPtr*)memory);
            return (T)gcHandle.Target;
        }
    }
}

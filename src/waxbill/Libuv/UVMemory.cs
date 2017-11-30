using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace waxbill.Libuv
{
    public class UVMemory : SafeHandle
    {
        public UVMemory():base(IntPtr.Zero,true)
        {

        }
        public override bool IsInvalid
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected override bool ReleaseHandle()
        {
            throw new NotImplementedException();
        }

        public void Validate(Boolean closed = false)
        {

        }

        public IntPtr InternalGetHandle()
        {
            return handle;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace waxbill
{

    /// <summary>
    /// 平台缓存
    /// </summary>
    public struct PlatformBuf
    {
        public readonly IntPtr Buffer;
        public readonly IntPtr Count;

        public PlatformBuf(IntPtr buffer, IntPtr count)
        {
            this.Buffer = buffer;
            this.Count = count;
        }
    }
}

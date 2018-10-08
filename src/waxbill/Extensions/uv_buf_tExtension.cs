using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Libuv;
using static waxbill.Libuv.UVIntrop;

namespace waxbill.Extensions
{
    public static class uv_buf_tExtension
    {
        /// <summary>
        /// 转换为平台相关包
        /// </summary>
        /// <param name="buf"></param>
        /// <returns></returns>
        public static PlatformBuf ToPlatformBuf(this uv_buf_t buf)
        {
            return new PlatformBuf(buf.Buffer, buf.Length);
        }

        public static uv_buf_t FromPlatformBuf(this PlatformBuf platformBuf)
        {
            return new uv_buf_t(platformBuf.Buffer, platformBuf.Count.ToInt32());
        }



    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZTImage.Libuv;
using static ZTImage.Libuv.UVIntrop;

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
            if (IsWindows)
            {
                return new PlatformBuf(buf._field1, buf._field0);
            }
            else
            {
                return new PlatformBuf(buf._field0, buf._field1);
            }
        }

        public static uv_buf_t FromPlatformBuf(this PlatformBuf platformBuf)
        {
            return new uv_buf_t(platformBuf.Buffer, platformBuf.Count.ToInt32(), UVIntrop.IsWindows);
        }



    }
}

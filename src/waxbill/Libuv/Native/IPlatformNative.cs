using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace waxbill.Libuv.Native
{
    /// <summary>
    /// 平台操作接口
    /// </summary>
    public interface IPlatformNative
    {
        /// <summary>
        /// 加载动态链接库
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        IntPtr LoadLibrary(string fileName);



        /// <summary>
        /// 移动一段内存
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="src"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        void MoveMemory(IntPtr dest, IntPtr src, uint size);
    }
}

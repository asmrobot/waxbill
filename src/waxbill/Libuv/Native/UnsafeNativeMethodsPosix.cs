using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace waxbill.Libuv.Native
{
    [SuppressUnmanagedCodeSecurity]
    internal class UnsafeNativeMethodsPosix: IPlatformNative
    {
        internal const int RTLD_DEFAULT = 0x102;
        internal const int RTLD_GLOBAL = 0x100;
        internal const int RTLD_LAZY = 1;
        internal const int RTLD_LOCAL = 0;
        internal const int RTLD_NOW = 2;

        [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true)]
        internal static extern IntPtr dlopen(string fileName, int mode);



        [DllImport("libm.so")]
        public static extern void memmove(IntPtr dest, IntPtr src, uint length);

        /// <summary>
        /// 非windows类加载
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public IntPtr LoadLibrary(string fileName)
        {
            return UnsafeNativeMethodsPosix.dlopen(fileName, 0x102);
        }


        /// <summary>
        /// 移动一段内存
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="src"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public void MoveMemory(IntPtr dest, IntPtr src, uint size)
        {
            memmove(dest, src, size);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace waxbill.Libuv.Native
{
    /// <summary>
    /// libuv动态库加载帮助类
    /// </summary>
    internal class NativeFactory
    {
        private static IPlatformNative nativeOperate;
        static NativeFactory()
        {
            if (UVIntrop.IsWindows)
            {
                nativeOperate = new UnsafeNativeMethodsWin32();
            }
            else
            {
                nativeOperate = new UnsafeNativeMethodsPosix();
            }

        }


        /// <summary>
        /// 加载动态库
        /// </summary>
        /// <param name="fileName">动态库路径</param>
        /// <returns></returns>
        public static IntPtr LoadLibrary(string fileName)
        {
            return nativeOperate.LoadLibrary(fileName);
        }

        /// <summary>
        /// 移动一段内存
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="src"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static void MoveMemory(IntPtr dest, IntPtr src, uint size)
        {
            nativeOperate.MoveMemory(dest, src, size);
        }



    }
}

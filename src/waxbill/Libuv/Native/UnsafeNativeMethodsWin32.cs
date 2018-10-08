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
    internal class UnsafeNativeMethodsWin32: IPlatformNative
    {
        [DllImport("kernel32", EntryPoint = "LoadLibrary", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr LoadLibraryWin(string fileName);



        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory", CharSet = CharSet.Ansi)]
        public extern static long MoveMemoryWin(IntPtr dest, IntPtr src, uint size);


        /// <summary>
        /// windows加载
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public IntPtr LoadLibrary(string fileName)
        {
            return LoadLibraryWin(fileName);
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
            MoveMemoryWin(dest, src, size);
        }
    }
}

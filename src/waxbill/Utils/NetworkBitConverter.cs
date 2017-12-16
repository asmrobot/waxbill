using System;
using System.Net;
using System.Runtime.InteropServices;

namespace waxbill.Utils
{
    /// <summary>
    /// network bit converter.
    /// </summary>
    static public class NetworkBitConverter
    {
        /// <summary>
        /// 以网络字节数组的形式返回指定的 16 位有符号整数值。
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static public byte[] GetBytes(Int16 value)
        {
            return BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
        }
        /// <summary>
        /// 以网络字节数组的形式返回指定的 32 位有符号整数值。
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static public byte[] GetBytes(Int32 value)
        {
            return BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
        }
        /// <summary>
        /// 以网络字节数组的形式返回指定的 64 位有符号整数值。
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static public byte[] GetBytes(Int64 value)
        {
            return BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
        }

        /// <summary>
        /// 返回由网络字节数组中指定位置的两个字节转换来的 16 位有符号整数。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        static public Int16 ToInt16(byte[] value, int startIndex)
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt16(value, startIndex));
        }
        /// <summary>
        /// 返回由网络字节数组中指定位置的四个字节转换来的 32 位有符号整数。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        static public Int32 ToInt32(byte[] value, int startIndex)
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(value, startIndex));
        }

        public static Int32 ToInt32(byte v1,byte v2,byte v3,byte v4)
        {
            convert32 c = new convert32();
            c.b1 = v4;
            c.b2 = v3;
            c.b3 = v2;
            c.b4 = v1;
            return c.source;
        }
        /// <summary>
        /// 返回由网络字节数组中指定位置的八个字节转换来的 64 位有符号整数。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        static public Int64 ToInt64(byte[] value, int startIndex)
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt64(value, startIndex));
        }


        [StructLayout(LayoutKind.Explicit)]
        public struct convert32
        {
            [FieldOffset(0)]
            public byte b1;
            [FieldOffset(1)]
            public byte b2;
            [FieldOffset(2)]
            public byte b3;
            [FieldOffset(3)]
            public byte b4;

            [FieldOffset(0)]
            public Int32 source;
        }
    }
}
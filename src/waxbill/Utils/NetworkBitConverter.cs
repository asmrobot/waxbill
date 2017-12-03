﻿using System;
using System.Net;

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
    }
}
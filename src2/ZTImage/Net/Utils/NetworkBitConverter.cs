namespace ZTImage.Net.Utils
{
    using System;
    using System.Net;

    public static class NetworkBitConverter
    {
        public static byte[] GetBytes(short value)
        {
            return BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
        }

        public static byte[] GetBytes(int value)
        {
            return BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
        }

        public static byte[] GetBytes(long value)
        {
            return BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
        }

        public static short ToInt16(byte[] value, int startIndex)
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt16(value, startIndex));
        }

        public static int ToInt32(byte[] value, int startIndex)
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(value, startIndex));
        }

        public static long ToInt64(byte[] value, int startIndex)
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt64(value, startIndex));
        }
    }
}


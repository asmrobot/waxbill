namespace ZTImage.Net.Protocols
{
    using System;
    using System.Runtime.InteropServices;
    using ZTImage.Net;
    using ZTImage.Net.Utils;

    public class RealProtocol : IProtocol
    {
        public bool TryToPacket(ref Packet packet, ArraySegment<byte> datas, out int readlen)
        {
            readlen = datas.Count;
            packet.Write(datas);
            return true;
        }
    }
}


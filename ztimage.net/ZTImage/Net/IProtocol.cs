namespace ZTImage.Net
{
    using System;
    using System.Runtime.InteropServices;
    using ZTImage.Net.Utils;

    public interface IProtocol
    {
        bool TryToPacket(ref Packet packet, ArraySegment<byte> datas, out int readlen);
    }
}


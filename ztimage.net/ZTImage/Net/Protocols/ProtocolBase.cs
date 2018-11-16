namespace ZTImage.Net.Protocols
{
    using System;
    using System.Runtime.InteropServices;
    using ZTImage.Net;
    using ZTImage.Net.Utils;

    public abstract class ProtocolBase : IProtocol
    {
        private int m_HeaderSize;

        public ProtocolBase(int headerSize)
        {
            this.m_HeaderSize = headerSize;
        }

        public abstract int IndexOfProtocolEnd(Packet packet, ArraySegment<byte> datas, out bool reset);
        public abstract bool ParseStart(Packet packet, ArraySegment<byte> datas, out bool reset);
        public bool TryToPacket(ref Packet packet, ArraySegment<byte> datas, out int readlen)
        {
            bool reset = false;
            readlen = 0;
            if (!packet.IsStart)
            {
                if (!this.ParseStart(packet, datas, out reset))
                {
                    readlen = datas.Count;
                    if (reset)
                    {
                        packet.Clear();
                    }
                    else
                    {
                        packet.Write(datas);
                    }
                    return false;
                }
                packet.IsStart = true;
            }
            readlen = this.IndexOfProtocolEnd(packet, datas, out reset);
            if (readlen < 0)
            {
                readlen = datas.Count;
                if (reset)
                {
                    packet.Clear();
                }
                else
                {
                    packet.Write(datas);
                }
                return false;
            }
            packet.Write(new ArraySegment<byte>(datas.Array, datas.Offset, readlen));
            return true;
        }

        public int HeaderSize
        {
            get
            {
                return this.m_HeaderSize;
            }
            protected set
            {
                this.m_HeaderSize = value;
            }
        }
    }
}


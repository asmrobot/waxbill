namespace ZTImage.Net.Protocols
{
    using System;
    using System.Runtime.InteropServices;
    using ZTImage.Net.Utils;

    public abstract class FixedHeaderProtocol : ProtocolBase
    {
        public FixedHeaderProtocol(int headerSize) : base(headerSize)
        {
            if (headerSize < 1)
            {
                throw new ArgumentNullException("headersize");
            }
        }

        public abstract int GetSize(byte[] datas);
        public override int IndexOfProtocolEnd(Packet packet, ArraySegment<byte> datas, out bool reset)
        {
            reset = false;
            if ((packet.Count + datas.Count) >= packet.ForecastSize)
            {
                return (packet.ForecastSize - packet.Count);
            }
            return -1;
        }

        public abstract bool IsStart(byte[] datas);
        public override bool ParseStart(Packet packet, ArraySegment<byte> datas, out bool reset)
        {
            reset = false;
            if ((packet.Count + datas.Count) < base.HeaderSize)
            {
                return false;
            }
            byte[] dst = new byte[base.HeaderSize];
            if (packet.Count > 0)
            {
                int num = Math.Min(base.HeaderSize, packet.Count);
                for (int i = 0; i < num; i++)
                {
                    dst[i] = packet[i];
                }
                if (packet.Count < base.HeaderSize)
                {
                    for (int j = 0; j < (base.HeaderSize - num); j++)
                    {
                        dst[num + j] = datas.Array[datas.Offset + j];
                    }
                }
            }
            else
            {
                Buffer.BlockCopy(datas.Array, datas.Offset, dst, 0, base.HeaderSize);
            }
            if (!this.IsStart(dst))
            {
                reset = true;
                return false;
            }
            packet.ForecastSize = this.GetSize(dst);
            if (packet.ForecastSize <= 0)
            {
                reset = true;
                return false;
            }
            packet.ForecastSize += base.HeaderSize;
            return true;
        }
    }
}


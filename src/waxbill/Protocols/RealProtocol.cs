using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Packets;
using waxbill.Utils;

namespace waxbill.Protocols
{
    /// <summary>
    /// 收到的全转发
    /// </summary>
    public class RealProtocol:IProtocol
    {
        public static readonly RealProtocol Define = new RealProtocol();

        private class RealtimeDataPacket : IPacket
        {

            public long Count
            {
                get
                {
                    return 0;
                }
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public byte[] Read()
            {
                throw new NotImplementedException();
            }

            public int Read(int sourceOffset, byte[] targetDatas, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public void Write(IntPtr memory, Int32 len)
            {

            }
        }

        public bool TryToPacket(ref IPacket packet, IntPtr memory, int len, out int readlen)
        {
            RealtimeDataPacket pack = packet as RealtimeDataPacket;
            if (pack == null)
            {
                throw new ArgumentException("packet is not realtimedatapacket");
            }
            readlen = len;
            pack.Write(memory, len);
            return true;
        }

        public IPacket CreatePacket()
        {
            return new RealtimeDataPacket();
        }
    }
}

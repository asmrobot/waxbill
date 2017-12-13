using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace waxbill.Packets
{
    public class RealtimeProtocolFrame : IPacket
    {
        public long Count => throw new NotImplementedException();

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
    }
}

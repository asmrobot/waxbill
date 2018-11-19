using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace waxbill.Pools
{
    public class PacketBufferPool:BufferPool
    {

        public PacketBufferPool(Int32 bufferSize,Int32 increases,Int32 max):base(bufferSize,increases,max)
        {

        }
        
    }
}

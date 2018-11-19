using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace waxbill.Pools
{
    public class ReceiveBufferPool:BufferPool
    {
        public ReceiveBufferPool(Int32 bufferSize, Int32 increases, Int32 max) : base(bufferSize, increases, max)
        {

        }
    }
}

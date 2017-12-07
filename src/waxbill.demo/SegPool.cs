using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Utils;

namespace waxbill.demo
{
    public class SegPool:PoolBase<ArraySegment<byte>>
    {
        public SegPool():base(50,0)
        {

        }

        protected override ArraySegment<byte> CreateItem(int index)
        {
            //byte[] b = new byte[12];
            ArraySegment<byte> b= new ArraySegment<byte>();
            return b;

        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Libuv;

namespace waxbill.Utils
{
    public class UVRequestPool : PoolBase<UVRequest>
    {
        public UVRequestPool():base(30,0)
        {}
        protected override UVRequest CreateItem(int index)
        {
            UVRequest request = new UVRequest(6);
            return request;
        }
    }
}

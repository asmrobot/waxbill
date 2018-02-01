using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Libuv;

namespace waxbill.Utils
{
    public class SendingPool : PoolBase<UVWriteRequest>
    {
        public SendingPool():base(30,0)
        {}
        protected override UVWriteRequest CreateItem(int index)
        {
            UVWriteRequest request = new UVWriteRequest();
            return request;
        }
    }
}

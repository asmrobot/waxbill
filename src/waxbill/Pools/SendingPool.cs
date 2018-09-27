using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZTImage.Libuv;

namespace waxbill.Utils
{
    public class SendingPool : PoolBase<UVWriteRequest>
    {
        public SendingPool():base(3,0)
        {}
        protected override UVWriteRequest CreateItem(int index)
        {
            UVWriteRequest request = new UVWriteRequest();
            return request;
        }
    }
}

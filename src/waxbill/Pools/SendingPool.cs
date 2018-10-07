using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Libuv;

namespace waxbill.Pools
{
    /// <summary>
    /// 发送队列池
    /// </summary>
    public class SendingQueuePool : PoolBase<UVWriteRequest>
    {
        public SendingQueuePool():base(3,0)
        {}
        protected override UVWriteRequest CreateItem(int index)
        {
            UVWriteRequest request = new UVWriteRequest();
            return request;
        }
    }
}

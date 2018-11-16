using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace waxbill.Pools
{
    /// <summary>
    /// 发送队列池
    /// </summary>
    public class SendingQueuePool : PoolBase<SendingQueue>
    {
        public SendingQueuePool():base(3,0)
        {}
        protected override SendingQueue CreateItem(int index)
        {
            SendingQueue request = new SendingQueue();
            return request;
        }
    }
}

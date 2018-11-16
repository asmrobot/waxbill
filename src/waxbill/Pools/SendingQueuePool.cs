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
            //todo:添加批量生成缓存的方法
            //SendingQueue request = new SendingQueue();
            //return request;
            return null;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize(Int32 minSendingPoolSize, Int32 maxSendingPoolSize, Int32 sendingQueueSize)
        {

        }
    }
}

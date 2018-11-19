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
        private readonly Int32 queueSize;
        public Int32 QueueSize
        {
            get
            {
                return queueSize;
            }
        }

        public SendingQueuePool(Int32 queueSize, Int32 increaseNumber,Int32 max) :base(increaseNumber,max)
        {
            this.queueSize = queueSize;
        }

        private List<ArraySegment<byte>[]> global = new List<ArraySegment<byte>[]>();

        protected override SendingQueue[] CreateItems(int suggestCount)
        {
            if (suggestCount <= 0)
            {
                throw new ArgumentOutOfRangeException("suggestcount<=0");
            }
            ArraySegment<byte>[] globalItem = new ArraySegment<byte>[suggestCount * this.queueSize];
            this.global.Add(globalItem);

            SendingQueue[] items = new SendingQueue[suggestCount];
            SendingQueue queue;
            for (int i = 0; i < suggestCount; i++)
            {
                queue = new SendingQueue(globalItem, i * this.queueSize, this.queueSize);
                items[i] = queue;
            }
            return items;
        }
    }
}

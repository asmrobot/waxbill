using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace waxbill.Utils
{
    /// <summary>
    ///发送队列产生池
    /// </summary>
    public class SendingPool
    {
        
        private int m_MinSendingQueueCount;
        /// <summary>
        /// 最小发送队列数
        /// </summary>
        public int MinSendingQueueCount
        {
            get
            {
                return m_MinSendingQueueCount;
            }
        }

        private int m_MaxsendingQueueCount;
        /// <summary>
        /// 最大发送队列数
        /// </summary>
        public int MaxSendingQueueCount
        {
            get
            {
                return m_MaxsendingQueueCount;
            }
        }

        private int m_TotalSendingQueueCount;
        /// <summary>
        /// 目前发送队列数
        /// </summary>
        public int TotalSendingQueueCount
        {
            get
            {
                return m_TotalSendingQueueCount;
            }
        }
        
        /// <summary>
        /// 空闲队列数
        /// </summary>
        public int IdleSendingQueueCount
        {
            get
            {
                return this.m_Pool.Count;
            }
        }


        /// <summary>
        /// 发送队列大小
        /// </summary>
        private int m_SendingQueueSize;
        public int SendingQueueSize
        {
            get
            {
                return m_SendingQueueSize;
            }
        }

        private ConcurrentStack<SendingQueue> m_Pool;
        private List<ArraySegment<byte>[]> m_SourceCoutainer;

        private int m_Increase = 0;

        

        public SendingPool()
        {
            this.m_Pool = new ConcurrentStack<SendingQueue>();
            this.m_SourceCoutainer = new List<ArraySegment<byte>[]>();
        }

        /// <summary>
        /// 初始化发送队列池
        /// </summary>
        /// <param name="_MinSendingQueueCount"></param>
        /// <param name="_MaxSendingQueueCount">0为不限</param>
        /// <param name="_SendingQueueSize"></param>
        public void Initialize(int _MinSendingQueueCount,int _MaxSendingQueueCount,int _SendingQueueSize)
        {
            if (_MinSendingQueueCount < 1)
            {
                throw new ArgumentOutOfRangeException("_MinSendingQueueCount不能小于1");
            }

            if (_MaxSendingQueueCount >0&&(_MaxSendingQueueCount<_MinSendingQueueCount))
            {
                throw new ArgumentOutOfRangeException("限制_MaxSendingQueueCount时，其不能小于_MinSendingQueueCount");
            }
            
            if (_SendingQueueSize < 1)
            {
                throw new ArgumentOutOfRangeException("_SendingQueueSize不能小于1");
            }

            
            this.m_MinSendingQueueCount = _MinSendingQueueCount;
            this.m_MaxsendingQueueCount = _MaxSendingQueueCount;
            this.m_SendingQueueSize = _SendingQueueSize;


            //初始化
            IncreaseCapity(_MinSendingQueueCount, _SendingQueueSize);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="counter">多少个队列</param>
        /// <param name="size">每个队列大小</param>
        private bool IncreaseCapity(int counter,int size)
        {
            if (this.m_MaxsendingQueueCount > 0)
            {
                counter = Math.Min(counter, this.m_MaxsendingQueueCount - this.TotalSendingQueueCount);
                if (counter <= 0)
                {
                    return false;
                }
            }
            
            int count = counter * size;
            ArraySegment<byte>[] items = new ArraySegment<byte>[count];
            this.m_SourceCoutainer.Add(items);

            for (int i = 0; i < counter; i++)
            {
                var queue = new SendingQueue(items, i * size, size);
                this.m_Pool.Push(queue);
            }
            m_TotalSendingQueueCount += counter;
            return true;
        }
        
        public bool TryGet(out SendingQueue queue)
        {
            if (this.m_Pool.TryPop(out queue))
            {
                return true;
            }
            
            while(true)
            {
                if (this.m_Pool.Count > 0)
                {
                    if (this.m_Pool.TryPop(out queue))
                    {
                        return true;
                    }
                    continue;
                }

                int increase = m_Increase;
                if (increase == 1)
                {
                    if (TryGetWithWait(out queue, 100))
                    {
                        return true;
                    }
                    continue;
                }

                if (Interlocked.CompareExchange(ref m_Increase, 1, increase) != increase)
                {
                    if (TryGetWithWait(out queue, 100))
                    {
                        return true;
                    }
                    continue;
                }

                bool result=IncreaseCapity(this.m_MinSendingQueueCount, this.m_SendingQueueSize);
                m_Increase = 0;
                if (!result)
                {
                    return false;
                }
                if (this.m_Pool.TryPop(out queue))
                {
                    return true;
                }
            }
        }


        private bool TryGetWithWait(out SendingQueue queue, int ticks)
        {
            SpinWait wait = new SpinWait();
            do
            {
                wait.SpinOnce();
                if (this.m_Pool.TryPop(out queue))
                {
                    return true;
                }

                if (wait.Count >= ticks)
                {
                    return false;
                }
            } while (true);
        }

        public void Push(SendingQueue queue)
        {
            this.m_Pool.Push(queue);
        }
    }
}

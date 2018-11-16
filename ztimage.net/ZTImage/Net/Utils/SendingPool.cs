namespace ZTImage.Net.Utils
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Threading;

    public class SendingPool
    {
        private int m_Increase;
        private int m_MaxsendingQueueCount;
        private int m_MinSendingQueueCount;
        private ConcurrentStack<SendingQueue> m_Pool = new ConcurrentStack<SendingQueue>();
        private int m_SendingQueueSize;
        private List<ArraySegment<byte>[]> m_SourceCoutainer = new List<ArraySegment<byte>[]>();
        private int m_TotalSendingQueueCount;

        private bool IncreaseCapity(int counter, int size)
        {
            if (this.m_MaxsendingQueueCount > 0)
            {
                counter = Math.Min(counter, this.m_MaxsendingQueueCount - this.TotalSendingQueueCount);
                if (counter <= 0)
                {
                    return false;
                }
            }
            ArraySegment<byte>[] item = new ArraySegment<byte>[counter * size];
            this.m_SourceCoutainer.Add(item);
            for (int i = 0; i < counter; i++)
            {
                SendingQueue queue = new SendingQueue(item, i * size, size);
                this.m_Pool.Push(queue);
            }
            this.m_TotalSendingQueueCount += counter;
            return true;
        }

        public void Initialize(int _MinSendingQueueCount, int _MaxSendingQueueCount, int _SendingQueueSize)
        {
            if (_MinSendingQueueCount < 1)
            {
                throw new ArgumentOutOfRangeException("_MinSendingQueueCount不能小于1");
            }
            if ((_MaxSendingQueueCount > 0) && (_MaxSendingQueueCount < _MinSendingQueueCount))
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
            this.IncreaseCapity(_MinSendingQueueCount, _SendingQueueSize);
        }

        public void Push(SendingQueue queue)
        {
            this.m_Pool.Push(queue);
        }

        public bool TryGet(out SendingQueue queue)
        {
            if (this.m_Pool.TryPop(out queue))
            {
                return true;
            }
        Label_0010:
            while (this.m_Pool.Count > 0)
            {
                if (this.m_Pool.TryPop(out queue))
                {
                    return true;
                }
            }
            int increase = this.m_Increase;
            if (increase == 1)
            {
                if (this.TryGetWithWait(out queue, 100))
                {
                    return true;
                }
                goto Label_0010;
            }
            if (Interlocked.CompareExchange(ref this.m_Increase, 1, increase) != increase)
            {
                if (this.TryGetWithWait(out queue, 100))
                {
                    return true;
                }
                goto Label_0010;
            }
            this.m_Increase = 0;
            if (!this.IncreaseCapity(this.m_MinSendingQueueCount, this.m_SendingQueueSize))
            {
                return false;
            }
            if (!this.m_Pool.TryPop(out queue))
            {
                goto Label_0010;
            }
            return true;
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
            }
            while (wait.Count < ticks);
            return false;
        }

        public int IdleSendingQueueCount
        {
            get
            {
                return this.m_Pool.Count;
            }
        }

        public int MaxSendingQueueCount
        {
            get
            {
                return this.m_MaxsendingQueueCount;
            }
        }

        public int MinSendingQueueCount
        {
            get
            {
                return this.m_MinSendingQueueCount;
            }
        }

        public int SendingQueueSize
        {
            get
            {
                return this.m_SendingQueueSize;
            }
        }

        public int TotalSendingQueueCount
        {
            get
            {
                return this.m_TotalSendingQueueCount;
            }
        }
    }
}


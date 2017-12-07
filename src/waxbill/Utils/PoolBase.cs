using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace waxbill.Utils
{
    public abstract class PoolBase<T>
    {
        private int mMinCount;
        /// <summary>
        /// 最小发送队列数
        /// </summary>
        public int MinCount
        {
            get
            {
                return mMinCount;
            }
        }

        private int mMaxCount;
        /// <summary>
        /// 最大发送队列数
        /// </summary>
        public int MaxCount
        {
            get
            {
                return mMaxCount;
            }
        }

        private int mCount;
        
        /// <summary>
        /// 目前发送队列数
        /// </summary>
        public int Count
        {
            get
            {
                return mCount;
            }
        }

        /// <summary>
        /// 空闲队列数
        /// </summary>
        public int IdleCount
        {
            get
            {
                return this.mPool.Count;
            }
        }

        private ConcurrentStack<T> mPool;
        private List<T[]> mSourceCoutainer;
        private int m_Increase = 0;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="minCount">最小数量</param>
        /// <param name="maxCount">最大数量（小于等于0为不限）</param>
        /// <param name="size"></param>
        public PoolBase(int minCount, int maxCount)
        {
            this.mPool = new ConcurrentStack<T>();
            this.mSourceCoutainer = new List<T[]>();
            
            if (minCount < 1)
            {
                throw new ArgumentOutOfRangeException("minCount不能小于1");
            }

            if (maxCount > 0 && (maxCount < minCount))
            {
                throw new ArgumentOutOfRangeException("限制maxCount时，其不能小于minCount");
            }


            this.mMinCount = minCount;
            this.mMaxCount = maxCount;
            
            //初始化
            IncreaseCapity(minCount);
        }

       
        /// <summary>
        /// 扩容数量
        /// </summary>
        /// <param name="count">多少个队列</param>
        /// <param name="size">每个队列大小</param>
        private bool IncreaseCapity(int count)
        {
            if (this.mMaxCount > 0)
            {
                count = Math.Min(count, this.mMaxCount - this.Count);
                if (count <= 0)
                {
                    return false;
                }
            }
            
            T[] items = new T[count];
            this.mSourceCoutainer.Add(items);

            for (int i = 0; i < count; i++)
            {
                T item = CreateItem(i);
                this.mPool.Push(item);
            }
            mCount += count;
            return true;
        }

        /// <summary>
        /// 尝试获取
        /// </summary>
        /// <param name="queue"></param>
        /// <returns></returns>
        public bool TryGet(out T queue)
        {
            if (this.mPool.TryPop(out queue))
            {
                return true;
            }

            while (true)
            {
                if (this.mPool.Count > 0)
                {
                    if (this.mPool.TryPop(out queue))
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

                bool result = IncreaseCapity(this.mMinCount);
                m_Increase = 0;
                if (!result)
                {
                    return false;
                }
                if (this.mPool.TryPop(out queue))
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// 尝试获取等待自旋ticks次后超时
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="ticks"></param>
        /// <returns></returns>
        private bool TryGetWithWait(out T queue, int ticks)
        {
            SpinWait wait = new SpinWait();
            do
            {
                wait.SpinOnce();
                if (this.mPool.TryPop(out queue))
                {
                    return true;
                }

                if (wait.Count >= ticks)
                {
                    return false;
                }
            } while (true);
        }

        /// <summary>
        /// 返回项
        /// </summary>
        /// <param name="queue"></param>
        public void Release(T queue)
        {
            this.mPool.Push(queue);
        }

        /// <summary>
        /// 创建项
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        protected abstract T CreateItem(Int32 index);
    }
}

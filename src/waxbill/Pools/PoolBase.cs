using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace waxbill.Pools
{
    /// <summary>
    /// 池基类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PoolBase<T>
    {

        public const Int32 SUGGEST_INCREASE_NUMBER_DEFAULT = 10;//默认建议每次增加数

        private int suggestIncrease;//建议增量
        /// <summary>
        /// 每次增加量
        /// </summary>
        public int Increases
        {
            get
            {
                return suggestIncrease;
            }
        }

        private int max;
        /// <summary>
        /// 池最大容量
        /// </summary>
        public int Max
        {
            get
            {
                return max;
            }
        }

        private int count;

        /// <summary>
        /// 目前池容量
        /// </summary>
        public int Count
        {
            get
            {
                return count;
            }
        }

        /// <summary>
        /// 池空闲容量
        /// </summary>
        public int IdleCount
        {
            get
            {
                return this.mPool.Count;
            }
        }

        private ConcurrentStack<T> mPool;
        private List<T[]> mArrayContainer;
        private int increase = 0;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="suggestIncrease">建议增加数量</param>
        /// <param name="max">最大数量（小于等于0为不限）</param>
        /// <param name="size"></param>
        public PoolBase(int suggestIncrease, int max)
        {
            if (suggestIncrease <= 0)
            {
                suggestIncrease = SUGGEST_INCREASE_NUMBER_DEFAULT;
            }

            if (max <= 0)
            {
                max = Int32.MaxValue;
            }

            this.mPool = new ConcurrentStack<T>();
            this.mArrayContainer = new List<T[]>();

            this.suggestIncrease = suggestIncrease;
            this.max = max;
        }


        /// <summary>
        /// 增加
        /// </summary>
        /// <param name="count">要增加的数量</param>
        private bool IncreaseCapity()
        {
            Int32 count = Math.Min(this.Increases, this.max - this.Count);
            if (count <= 0)
            {
                return false;
            }

            T[] items = CreateItems(count);
            if (items == null || items.Length <= 0)
            {
                throw new Exception("pool can't allow");
            }
            this.mArrayContainer.Add(items);

            for (int i = 0; i < items.Length; i++)
            {
                this.mPool.Push(items[i]);
            }
            this.count += count;
            return true;
        }
        
        /// <summary>
        /// 尝试获取
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public virtual bool TryGet(out T val)
        {
            val = default(T);
            //todo:remove
            //if (this.mPool.TryPop(out val))
            //{
            //    return true;
            //}

            while (true)
            {
                if (this.mPool.Count > 0)
                {
                    if (this.mPool.TryPop(out val))
                    {
                        return true;
                    }
                    continue;
                }

                int increase = this.increase;
                if (increase == 1)
                {
                    if (TryGet(out val, 100))
                    {
                        return true;
                    }
                    continue;
                }

                if (Interlocked.CompareExchange(ref this.increase, 1, increase) != increase)
                {
                    if (TryGet(out val, 100))
                    {
                        return true;
                    }
                    continue;
                }

                bool result = IncreaseCapity();
                this.increase = 0;
                if (!result)
                {
                    return false;
                }

                //todo:remove
                //if (this.mPool.TryPop(out val))
                //{
                //    return true;
                //}
            }
        }

        /// <summary>
        /// 尝试获取自旋tryCounter次后超时
        /// </summary>
        /// <param name="val">获取值</param>
        /// <param name="tryCounter">尝试次数</param>
        /// <returns></returns>
        private bool TryGet(out T val, int tryCounter)
        {
            SpinWait wait = new SpinWait();
            do
            {
                wait.SpinOnce();
                if (this.mPool.TryPop(out val))
                {
                    return true;
                }

                if (wait.Count >= tryCounter)
                {
                    return false;
                }
            } while (true);
        }

        /// <summary>
        /// 释放
        /// </summary>
        /// <param name="val"></param>
        public virtual void Release(T val)
        {
            this.mPool.Push(val);
        }


        /// <summary>
        /// 创建项
        /// </summary>
        /// <param name="suggestCount"></param>
        /// <returns></returns>
        protected abstract T[] CreateItems(Int32 suggestCount);
    }
}

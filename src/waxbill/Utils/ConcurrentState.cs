using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace waxbill.Utils
{
    /// <summary>
    /// 可并发的状态
    /// </summary>
    public struct ConcurrentState
    {
        public Int32 Value;

        public ConcurrentState(Int32 current)
        {
            Value = current;
        }

        public void SetState(int state)
        {
            this.SetState(state, -1);
        }

        /// <summary>
        /// 设置状态
        /// </summary>
        /// <param name="state">要设置的状态值</param>
        /// <param name="careState">是否在意某状态，,-1为不在意任何状态,其它值为在意。在意某状态时如果存在某状态则不设置状态</param>
        /// <returns></returns>
        public bool SetState(int state, Int32 careState)
        {
            int sourceState;
            int num2;
            do
            {
                sourceState = this.Value;
                if (careState>-1 && ((sourceState&careState)==careState))
                {
                    return false;
                }
                num2 = this.Value | state;
            }
            while (Interlocked.CompareExchange(ref this.Value, num2, sourceState) != sourceState);
            return true;
        }

        /// <summary>
        /// 尝试设置某状态
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool TrySetState(int state)
        {
            int mState;
            int num2;
            do
            {
                mState = this.Value;
                num2 = mState | state;
                if (num2 == this.Value)
                {
                    return false;
                }
            }
            while (Interlocked.CompareExchange(ref this.Value, num2, mState) != mState);
            return true;
        }

        /// <summary>
        /// 移除某一状态
        /// </summary>
        /// <param name="state"></param>
        public void RemoveState(int state)
        {
            int mState;
            int num2;
            do
            {
                mState = this.Value;
                num2 = mState & ~state;
            }
            while (Interlocked.CompareExchange(ref this.Value, num2, mState) != mState);
        }

        /// <summary>
        /// 获取是否有某种状态
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool GetState(int state)
        {
            return ((this.Value & state) == state);
        }


        #region 操作符重载
        public static Boolean operator==(ConcurrentState self, Int32 val)
        {
            if (self.Value == val)
            {
                return true;
            }
            return false;
        }


        public static Boolean operator!=(ConcurrentState self, Int32 val)
        {
            if (self.Value == val)
            {
                return false;
            }
            return true;
        }


        public static Boolean operator>(ConcurrentState self, Int32 val)
        {
            if (self.Value > val)
            {
                return true;
            }
            return false;
        }

        public static Boolean operator <(ConcurrentState self, Int32 val)
        {
            if (self.Value < val)
            {
                return true;
            }
            return false;
        }

        public static Boolean operator >=(ConcurrentState self, Int32 val)
        {
            if (self.Value >= val)
            {
                return true;
            }
            return false;
        }

        public static Boolean operator <=(ConcurrentState self, Int32 val)
        {
            if (self.Value <= val)
            {
                return true;
            }
            return false;
        }
        #endregion



        public override bool Equals(object obj)
        {
            try
            {
                ConcurrentState state = (ConcurrentState)obj;
                return state == this.Value;
            }
            catch
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

    }
}

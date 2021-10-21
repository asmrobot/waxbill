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
        private readonly Int32 _defaultValue;

        public ConcurrentState(Int32 current)
        {
            this._defaultValue = current;
            Value = current;
        }

        /// <summary>
        /// 设置状态
        /// </summary>
        /// <param name="state">要设置的状态值</param>
        /// <param name="careState">是否在意某状态，,-1为不在意任何状态,其它值为在意。在意某状态时如果存在某状态则不设置状态</param>
        /// <returns>true:为本线程设置成功的，false:非本线程设置成功的或存在在意状态</returns>
        public bool SetState(int state, Int32 careState)
        {
            int currentState;
            int newState;
            do
            {
                currentState = this.Value;
                //在意状态
                if (careState>-1 && ((currentState&careState)==careState))
                {
                    return false;
                }
                //已经设置,并不是自已设置的
                if ((currentState & state) == state)
                {
                    return true;
                }
                newState = currentState | state;
            }
            while (Interlocked.CompareExchange(ref this.Value, newState, currentState) != currentState);
            return true;
        }

        /// <summary>
        /// 设置某状态,并返回是否是自已设置成功的
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool SetState(int state)
        {
            return SetState(state, -1);
        }

        /// <summary>
        /// 移除某一状态
        /// </summary>
        /// <param name="state"></param>
        public void RemoveState(int state)
        {
            int currentState;
            int newState;
            do
            {
                currentState = this.Value;
                newState = currentState & ~state;
            }
            while (Interlocked.CompareExchange(ref this.Value, newState, currentState) != currentState);
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

        /// <summary>
        /// 重置
        /// </summary>
        /// <returns></returns>
        public void Reset()
        {
            this.Value = _defaultValue;
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

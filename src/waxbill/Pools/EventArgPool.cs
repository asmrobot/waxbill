using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Collections.Concurrent;

namespace waxbill.Utils
{

    public class EventArgPool
    {
        public int ReceiveBufferSize { get; set; }
        private Int32 mPoolSize;

        public EventArgPool(int receiveBufferSize,Int32 poolSize)
        {
            
            this.ReceiveBufferSize = receiveBufferSize;
            if (poolSize <= 0)
            {
                mPoolSize = Int32.MaxValue;
            }
            else
            {
                this.mPoolSize = poolSize;
            }
            
        }
        #region Pool
        private readonly ConcurrentStack<SocketAsyncEventArgs> _PoolStack = new ConcurrentStack<SocketAsyncEventArgs>();

        internal SocketAsyncEventArgs GetSocketAsyncEventArgs()
        {
            SocketAsyncEventArgs e;
            if (!this._PoolStack.TryPop(out e))
            {
                e = new SocketAsyncEventArgs();

                //byte[] datas = BufferManager.Instance.GetBuffer(out offset, out size);
                byte[] datas = new byte[this.ReceiveBufferSize];
                e.SetBuffer(datas, 0, datas.Length);
            }


            return e;
        }

        internal void RealseSocketAsyncEventArgs(SocketAsyncEventArgs e)
        {

            if (e.Buffer == null || e.Buffer.Length != this.ReceiveBufferSize)
            {
                e.Dispose();
                return;
            }


            if (this._PoolStack.Count >= this.mPoolSize)
            {

                //BufferManager.Instance.FreeBuffer(e.Buffer, e.Offset);
                e.SetBuffer(null, 0, 0);

                e.Dispose();
                return;
            }
            
            this._PoolStack.Push(e);
        }

        #endregion
    }
}

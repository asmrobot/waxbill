using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace waxbill.Pools
{
    public class EventArgPool: PoolBase<SocketAsyncEventArgs>
    {
        public EventArgPool(Int32 increaseNumber, Int32 max) : base(increaseNumber, max)
        { }
        

        protected override SocketAsyncEventArgs[] CreateItems(int suggestCount)
        {
            SocketAsyncEventArgs[] items = new SocketAsyncEventArgs[suggestCount];
            for (int i = 0; i < suggestCount; i++)
            {
                items[i] = new SocketAsyncEventArgs();
            }
            return items;
        }

    }
}

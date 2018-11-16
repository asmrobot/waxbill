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
        public EventArgPool(SocketConfiguration config) : base(3, 0)
        { }
        protected override SocketAsyncEventArgs CreateItem(int index)
        {
            SocketAsyncEventArgs eventArgs = new SocketAsyncEventArgs();
            return eventArgs;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using waxbill.Libuv;
using waxbill.Packets;
using waxbill.Utils;

namespace waxbill
{
    public class TCPMonitor
    {

        /// <summary>
        /// 连接事件
        /// </summary>
        public event OnConnectionEvent OnConnection;
        public ServerOption Option { get; private set; }
        internal IProtocol Protocol { get; private set; }
        internal UVRequestPool SendPool;
        internal BufferManager BufferManager;


        public TCPMonitor(IProtocol protocol,ServerOption option)
        {
            Validate.ThrowIfNull(protocol, "协议为空");
            Validate.ThrowIfNull(option, "服务配置参数不正确");
            this.Protocol = protocol;
            this.Option = option;
            this.SendPool = new UVRequestPool();
            this.BufferManager = new BufferManager(this.Option.BufferSize, this.Option.BufferIncemerCount);
        }

        

        #region Events
        internal void RaiseOnConnectionEvent(SocketSession session)
        {
            if (OnConnection != null)
            {
                OnConnection(session);
            }
        }

        /// <summary>
        /// 断开连接事件
        /// </summary>
        public event OnDisconnectedEvent OnDisconnected;

        internal void RaiseOnDisconnectedEvent(SocketSession session, CloseReason reason)
        {
            if (this.OnDisconnected != null)
            {
                OnDisconnected(session, reason);
            }
        }

        /// <summary>
        /// 发送事件
        /// </summary>
        public event OnSendedEvent OnSended;
        internal void RaiseOnSendedEvent(SocketSession session, IList<UVIntrop.uv_buf_t> packet, bool result)
        {
            if (this.OnSended != null)
            {
                OnSended(session, packet, result);
            }
        }


        /// <summary>
        /// 接收事件
        /// </summary>
        public event OnReceiveEvent OnReceive;
        internal void RaiseOnReceiveEvent(SocketSession session, Packet collection)
        {
            if (OnReceive != null)
            {
                OnReceive(session, collection);
            }
        }

        #endregion
    }
}

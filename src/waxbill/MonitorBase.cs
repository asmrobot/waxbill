﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using waxbill.Libuv;
using waxbill.Packets;
using waxbill.Sessions;
using waxbill.Utils;

namespace waxbill
{
    public abstract class MonitorBase
    {
        internal IProtocol Protocol
        {
            get;
            private set;
        }
        
        public TCPOption Option { get; private set; }

        public BufferManager BufferManager { get; private set; }


        public MonitorBase(IProtocol protocol,TCPOption option, BufferManager mBufferManager)
        {
            Validate.ThrowIfNull(protocol, "协议为空");
            Validate.ThrowIfNull(mBufferManager, "mBufferManager");
            this.Protocol = protocol;
            this.Option = option;
            this.BufferManager = mBufferManager;
        }


        
        /// <summary>
        /// 获得发送队列
        /// </summary>
        /// <param name="queue"></param>
        /// <returns></returns>
        public abstract bool TryGetSendQueue(out UVRequest queue);

        /// <summary>
        /// 释放发送队列
        /// </summary>
        /// <param name="queue"></param>
        public abstract void ReleaseSendQueue(UVRequest queue);

        /// <summary>
        /// 获取接收缓存
        /// </summary>
        /// <returns></returns>
        public abstract bool TryGetReceiveMemory(out IntPtr memory);

        /// <summary>
        /// 释放接收缓存
        /// </summary>
        /// <param name="memory"></param>
        public abstract void ReleaseReceiveMemory(IntPtr memory);

        /// <summary>
        /// 创建一个数据包
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public Packet CreatePacket()
        {
            return new Packet(this.BufferManager);
        }
    }
}
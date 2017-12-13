﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Packets;
using waxbill.Utils;

namespace waxbill.Protocols
{
    /// <summary>
    /// 收到的全转发
    /// </summary>
    public class RealtimeProtocol:IProtocol<RealtimeProtocolFrame>
    {
        public static readonly RealtimeProtocol Define = new RealtimeProtocol();
       
        public bool TryToPacket(ref RealtimeProtocolFrame packet, IntPtr datas, int count, out int giveupCount)
        {
            giveupCount = count;
            if (count <= 0)
            {
                return true;
            }
            
            //pack.Write(datas, count);

            return true;
        }

        public RealtimeProtocolFrame CreatePacket()
        {
            return null;
        }
    }
}
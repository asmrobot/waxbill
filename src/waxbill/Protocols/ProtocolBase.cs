﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using waxbill.Utils;
namespace waxbill.Protocols
{
    public abstract class ProtocolBase:IProtocol
    {
        private Int32 m_HeaderSize;
        public Int32 HeaderSize
        {
            get
            {
                return m_HeaderSize;
            }
            protected set
            {
                m_HeaderSize = value;
            }
        }

        public ProtocolBase(Int32 headerSize)
        {
            this.m_HeaderSize = headerSize;
        }
        
        /// <summary>
        /// 字节转包
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="datas"></param>
        /// <param name="readlen">读取长度</param>
        /// <returns>转包是否成功</returns>
        public bool TryToMessage(ref Packet packet, ArraySegment<byte> datas, out int readlen)
        {
            bool reset = false;
            readlen = 0;
            
            //处理开始
            if (!packet.IsStart)
            {
                if (!ParseStart(packet, datas,out reset))
                {
                    readlen = datas.Count;
                    if (reset)
                    {
                        packet.Clear();
                    }
                    else
                    {
                        packet.Write(datas);
                    }
                    return false;
                }
                packet.IsStart = true;
            }
            

            readlen = IndexOfProtocolEnd(packet, datas, out reset);
            if (readlen<0)
            {
                readlen = datas.Count;
                if (reset)
                {
                    packet.Clear();
                }
                else
                {
                    packet.Write(datas);
                }
                return false;
            }

            packet.Write(new ArraySegment<byte>(datas.Array, datas.Offset, readlen));
            return true;
        }



        /// <summary>
        /// 是否成功解析开始
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="datas"></param>
        /// <param name="readlen"></param>
        /// <returns></returns>
        public abstract bool ParseStart(Packet packet,ArraySegment<byte> datas,out bool reset);
        

        /// <summary>
        /// 解析结束
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="datas"></param>
        /// <param name="reset"></param>
        /// <returns>没有查找到结束，则返回-1</returns>
        public abstract Int32 IndexOfProtocolEnd(Packet packet,ArraySegment<byte> datas, out bool reset);
    }
}
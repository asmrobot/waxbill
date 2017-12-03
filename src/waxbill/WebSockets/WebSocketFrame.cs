using System;
using System.Collections.Generic;
using System.IO;
namespace waxbill.WebSockets
{
    public enum Opcode : byte
    {
        ContinuationFrame = 0,
        TextFrame = 1,
        BinaryFrame = 2,
        ConnectionCloseFrame = 8,
        PingFrame = 9,
        PongFrame = 10

    }
    public class WebSocketFrame
    {

        /// <summary>
        /// 1位，表示信息的最后一帧，flag,也就是标记符
        /// </summary>
        public byte Fin { get; set; }

        /// <summary>
        /// 1位，以后备用的，默认都为0
        /// </summary>
        public byte Rsv1 { get; set; }

        public byte Rsv2 { get; set; }

        public byte Rsv3 { get; set; }

        /// <summary>
        /// 4位,帧类型
        /// </summary>
        public Opcode Opcode { get; set; }

        public bool IsString
        {
            get
            {
                return Opcode == Opcode.TextFrame;
            }
        }

        /// <summary>
        /// 1位,是否加密数据，默认必须置为1
        /// </summary>
        public byte Mask { get; set; }

        /// <summary>
        /// 7bit,数据的长度
        /// </summary>
        public byte Payload_len { get; set; }

        /// <summary>
        /// 数据长度
        /// </summary>
        public Int32 FrameLength { get; set; }

        public byte[] Masking_key { get; set; }


        public ArraySegment<byte> Payload_data { get; set; }

        public ArraySegment<byte> Extension_data { get; set; }

        public ArraySegment<byte> Application_data { get; set; }

        /// <summary>
        /// 得到请求模型 lhg
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        public static WebSocketFrame GetFrame(byte[] datas)
        {
            WebSocketFrame frame = new WebSocketFrame();
            int i = 0;
            frame.Fin = (byte)(datas[i] >> 7);

            byte opcode_v = (byte)(datas[i++] & 0x0F);

            try
            {
                frame.Opcode = (Opcode)opcode_v;
            }
            catch
            {
                return null;
            }



            frame.Mask = (byte)(datas[i] >> 7);
            frame.Payload_len = (byte)(datas[i++] & 0x7F);
            if (frame.Payload_len < 126)
            {
                frame.FrameLength = frame.Payload_len;
            }
            else if (frame.Payload_len == 126)
            {
                frame.FrameLength = datas[i++] << 8 + datas[i++];
            }
            else if (frame.Payload_len == 127)
            {
                i += 4;
                frame.FrameLength = (datas[i++] << 24) + (datas[i++] << 16) + (datas[i++] << 8) + datas[i++];
            }

            if (frame.Mask == 1)
            {
                frame.Masking_key = new byte[] { datas[i++], datas[i++], datas[i++], datas[i++] };
                for (int j = 0; j < frame.FrameLength; j++)
                {
                    datas[i + j] = (byte)(datas[i + j] ^ frame.Masking_key[j % 4]);
                }
                frame.Payload_data = new ArraySegment<byte>(datas, i, frame.FrameLength);
            }
            else
            {
                frame.Payload_data = new ArraySegment<byte>(datas, i, frame.FrameLength);
            }

            return frame;
        }


        /// <summary>
        /// 得到请求模型 lhg
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        public static WebSocketFrame GetFrameFromStream(Stream stream)
        {
            WebSocketFrame frame = new WebSocketFrame();
            byte[] datas = new byte[1024];
            int _RecCount = 0;
            
            _RecCount = LoopReadFormStream(stream,datas, 0, 2);
            if (_RecCount != 2)
            {
                //协议错误
                return null;
            }
            int i = 0;
            
            frame.Fin = (byte)(datas[i] >> 7);

            byte opcode_v = (byte)(datas[i++] & 0x0F);

            try
            {
                frame.Opcode = (Opcode)opcode_v;
            }
            catch
            {
                return null;
            }



            frame.Mask = (byte)(datas[i] >> 7);
            frame.Payload_len = (byte)(datas[i++] & 0x7F);
            i = 0;

            if (frame.Payload_len < 126)
            {
                frame.FrameLength = frame.Payload_len;
            }
            else
            {
                if (frame.Payload_len == 126)
                {
                    _RecCount = LoopReadFormStream(stream, datas, 0, 2);
                    if (_RecCount != 2)
                    {
                        return null;
                    }
                    i = 0;
                    frame.FrameLength = datas[i++] << 8 + datas[i++];
                }
                else if (frame.Payload_len == 127)
                {
                    _RecCount = LoopReadFormStream(stream, datas, 0, 8);
                    if (_RecCount != 8)
                    {
                        return null;
                    }
                    i = 4;
                    frame.FrameLength = (datas[i++] << 24) + (datas[i++] << 16) + (datas[i++] << 8) + datas[i++];
                }
            }

            if (frame.Mask == 1)
            {
                _RecCount = LoopReadFormStream(stream, datas, 0, 4);
                if (_RecCount != 4)
                {
                    return null;
                }
                i = 0;

                frame.Masking_key = new byte[] { datas[i++], datas[i++], datas[i++], datas[i++] };
            }

            byte[] tempd = new byte[frame.FrameLength];
            _RecCount = LoopReadFormStream(stream, tempd, 0, frame.FrameLength);
            if (_RecCount != frame.FrameLength)
            {
                return null;
            }

            if (frame.Mask == 1)
            {
                for (int j = 0; j < frame.FrameLength; j++)
                {
                    tempd[j] = (byte)(tempd[j] ^ frame.Masking_key[j % 4]);
                }
            }
            frame.Payload_data = new ArraySegment<byte>(tempd, 0, frame.FrameLength);
            return frame;
        }

        private static Int32 LoopReadFormStream(Stream stream,byte[] datas,int offset,int count)
        {
            if (datas == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (datas == null)
            {
                throw new ArgumentNullException("datas");
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            if (offset + count > datas.Length)
            {
                throw new ArgumentOutOfRangeException("offset+count");
            }

            int readlen = 0;
            while (true)
            {
                int rc=stream.Read(datas, offset + readlen, count - readlen);
                if (rc <= 0)
                {
                    break;
                }
                readlen += rc;
                if (readlen >= count)
                {
                    break;
                }
            }
            return readlen;

        }

        /// <summary>
        /// 序列化为二进制数据
        /// </summary>
        /// <returns></returns>
        public byte[] GetDatas()
        {
            return GetDatas(this.Fin, this.Opcode, this.Payload_data);
        }

        /// <summary>
        /// 序列化为二进制数据
        /// </summary>
        /// <returns></returns>
        public static byte[] GetDatas(byte fin, Opcode opcode, ArraySegment<byte> payload_datas)
        {
            List<byte> s = new List<byte>();
            Int32 l = payload_datas.Count;
            //第一个字节
            s.Add((byte)((fin << 7) + (byte)opcode));

            //第二个字节
            if (l < 126)
            {
                s.Add((byte)l);
            }
            else if (l < 0x010000)
            {
                s.Add(126);
                s.Add((byte)((l & 0xff00) >> 2));
                s.Add((byte)(l & 0xff));
            }
            else
            {
                s.AddRange(new byte[] {
                    127,0,0,0,0,
                    (byte)((l&0xFF000000)>>6),(byte)((l&0xFF0000)>>4),(byte)((l&0xFF00)>>2),(byte)(l&0xFF)
                });
            }
            Int32 all_len = s.Count + l;
            byte[] datas = new byte[all_len];
            for (int i = 0; i < s.Count; i++)
            {
                datas[i] = s[i];
            }

            Buffer.BlockCopy(payload_datas.Array, payload_datas.Offset, datas, s.Count, payload_datas.Count);
            return datas;
        }



        public override string ToString()
        {
            return String.Format("fin:{0},opcode:{1},mask:{2},payload_len:{3}", this.Fin, this.Opcode, this.Mask, this.Payload_len);
        }

    }
}

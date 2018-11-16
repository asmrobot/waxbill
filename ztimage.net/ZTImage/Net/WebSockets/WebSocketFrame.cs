namespace ZTImage.Net.WebSockets
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.CompilerServices;

    public class WebSocketFrame
    {
        public byte[] GetDatas()
        {
            return GetDatas(this.Fin, this.Opcode, this.Payload_data);
        }

        public static byte[] GetDatas(byte fin, ZTImage.Net.WebSockets.Opcode opcode, ArraySegment<byte> payload_datas)
        {
            List<byte> list = new List<byte>();
            int count = payload_datas.Count;
            list.Add((byte) ((fin << 7) + ((int) opcode)));
            if (count < 0x7e)
            {
                list.Add((byte) count);
            }
            else if (count < 0x10000)
            {
                list.Add(0x7e);
                list.Add((byte) ((count & 0xff00) >> 2));
                list.Add((byte) (count & 0xff));
            }
            else
            {
                byte[] collection = new byte[9];
                collection[0] = 0x7f;
                collection[5] = (byte) ((count & 0xff000000L) >> 6);
                collection[6] = (byte) ((count & 0xff0000) >> 4);
                collection[7] = (byte) ((count & 0xff00) >> 2);
                collection[8] = (byte) (count & 0xff);
                list.AddRange(collection);
            }
            byte[] dst = new byte[list.Count + count];
            for (int i = 0; i < list.Count; i++)
            {
                dst[i] = list[i];
            }
            Buffer.BlockCopy(payload_datas.Array, payload_datas.Offset, dst, list.Count, payload_datas.Count);
            return dst;
        }

        public static WebSocketFrame GetFrame(byte[] datas)
        {
            WebSocketFrame frame = new WebSocketFrame();
            int index = 0;
            frame.Fin = (byte) (datas[index] >> 7);
            byte num2 = (byte) (datas[index++] & 15);
            try
            {
                frame.Opcode = (ZTImage.Net.WebSockets.Opcode) num2;
            }
            catch
            {
                return null;
            }
            frame.Mask = (byte) (datas[index] >> 7);
            frame.Payload_len = (byte) (datas[index++] & 0x7f);
            if (frame.Payload_len < 0x7e)
            {
                frame.FrameLength = frame.Payload_len;
            }
            else if (frame.Payload_len == 0x7e)
            {
                frame.FrameLength = datas[index++] << (8 + datas[index++]);
            }
            else if (frame.Payload_len == 0x7f)
            {
                index += 4;
                frame.FrameLength = (((datas[index++] << 0x18) + (datas[index++] << 0x10)) + (datas[index++] << 8)) + datas[index++];
            }
            if (frame.Mask == 1)
            {
                frame.Masking_key = new byte[] { datas[index++], datas[index++], datas[index++], datas[index++] };
                for (int i = 0; i < frame.FrameLength; i++)
                {
                    datas[index + i] = (byte) (datas[index + i] ^ frame.Masking_key[i % 4]);
                }
                frame.Payload_data = new ArraySegment<byte>(datas, index, frame.FrameLength);
                return frame;
            }
            frame.Payload_data = new ArraySegment<byte>(datas, index, frame.FrameLength);
            return frame;
        }

        public static WebSocketFrame GetFrameFromStream(Stream stream)
        {
            WebSocketFrame frame = new WebSocketFrame();
            byte[] datas = new byte[0x400];
            if (LoopReadFormStream(stream, datas, 0, 2) != 2)
            {
                return null;
            }
            int index = 0;
            frame.Fin = (byte) (datas[index] >> 7);
            byte num2 = (byte) (datas[index++] & 15);
            try
            {
                frame.Opcode = (ZTImage.Net.WebSockets.Opcode) num2;
            }
            catch
            {
                return null;
            }
            frame.Mask = (byte) (datas[index] >> 7);
            frame.Payload_len = (byte) (datas[index++] & 0x7f);
            index = 0;
            if (frame.Payload_len < 0x7e)
            {
                frame.FrameLength = frame.Payload_len;
            }
            else if (frame.Payload_len == 0x7e)
            {
                if (LoopReadFormStream(stream, datas, 0, 2) != 2)
                {
                    return null;
                }
                index = 0;
                frame.FrameLength = datas[index++] << (8 + datas[index++]);
            }
            else if (frame.Payload_len == 0x7f)
            {
                if (LoopReadFormStream(stream, datas, 0, 8) != 8)
                {
                    return null;
                }
                index = 4;
                frame.FrameLength = (((datas[index++] << 0x18) + (datas[index++] << 0x10)) + (datas[index++] << 8)) + datas[index++];
            }
            if (frame.Mask == 1)
            {
                if (LoopReadFormStream(stream, datas, 0, 4) != 4)
                {
                    return null;
                }
                index = 0;
                frame.Masking_key = new byte[] { datas[index++], datas[index++], datas[index++], datas[index++] };
            }
            byte[] buffer2 = new byte[frame.FrameLength];
            if (LoopReadFormStream(stream, buffer2, 0, frame.FrameLength) != frame.FrameLength)
            {
                return null;
            }
            if (frame.Mask == 1)
            {
                for (int i = 0; i < frame.FrameLength; i++)
                {
                    buffer2[i] = (byte) (buffer2[i] ^ frame.Masking_key[i % 4]);
                }
            }
            frame.Payload_data = new ArraySegment<byte>(buffer2, 0, frame.FrameLength);
            return frame;
        }

        private static int LoopReadFormStream(Stream stream, byte[] datas, int offset, int count)
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
            if ((offset + count) > datas.Length)
            {
                throw new ArgumentOutOfRangeException("offset+count");
            }
            int num = 0;
            do
            {
                int num2 = stream.Read(datas, offset + num, count - num);
                if (num2 <= 0)
                {
                    return num;
                }
                num += num2;
            }
            while (num < count);
            return num;
        }

        public override string ToString()
        {
            object[] args = new object[] { this.Fin, this.Opcode, this.Mask, this.Payload_len };
            return string.Format("fin:{0},opcode:{1},mask:{2},payload_len:{3}", args);
        }

        public ArraySegment<byte> Application_data { get; set; }

        public ArraySegment<byte> Extension_data { get; set; }

        public byte Fin { get; set; }

        public int FrameLength { get; set; }

        public bool IsString
        {
            get
            {
                return (this.Opcode == ZTImage.Net.WebSockets.Opcode.TextFrame);
            }
        }

        public byte Mask { get; set; }

        public byte[] Masking_key { get; set; }

        public ZTImage.Net.WebSockets.Opcode Opcode { get; set; }

        public ArraySegment<byte> Payload_data { get; set; }

        public byte Payload_len { get; set; }

        public byte Rsv1 { get; set; }

        public byte Rsv2 { get; set; }

        public byte Rsv3 { get; set; }
    }
}


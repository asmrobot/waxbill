namespace ZTImage.Net.Utils
{
    using System;
    using System.IO;

    public class PacketStream : Stream
    {
        private ZTImage.Net.Utils.Packet m_Packet;
        private int m_Position;

        public PacketStream(ZTImage.Net.Utils.Packet packet)
        {
            this.m_Packet = packet;
            this.m_Position = 0;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if ((offset + count) > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset+count ");
            }
            if (this.m_Position >= this.m_Packet.Count)
            {
                return 0;
            }
            int num = this.m_Packet.Read(this.m_Position, buffer, offset, count);
            this.m_Position += num;
            return num;
        }

        public override long Seek(long offset, SeekOrigin loc)
        {
            switch (loc)
            {
                case SeekOrigin.Begin:
                    if ((offset < 0L) || (offset >= this.m_Packet.Count))
                    {
                        throw new IOException("seek不在范围内");
                    }
                    this.m_Position = (int) offset;
                    break;

                case SeekOrigin.Current:
                {
                    int num = this.m_Position + ((int) offset);
                    if (num > this.m_Packet.Count)
                    {
                        throw new IOException("seek 太大");
                    }
                    this.m_Position = num;
                    break;
                }
                case SeekOrigin.End:
                {
                    int num2 = this.m_Packet.Count + ((int) offset);
                    if ((num2 < 0) || (num2 >= this.m_Packet.Count))
                    {
                        throw new IOException("seek不在范围内");
                    }
                    this.m_Position = num2;
                    break;
                }
                default:
                    throw new ArgumentException("Argument_InvalidSeekOrigin");
            }
            return (long) this.m_Position;
        }

        public override void SetLength(long value)
        {
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                return (long) this.m_Packet.Count;
            }
        }

        public ZTImage.Net.Utils.Packet Packet
        {
            get
            {
                return this.m_Packet;
            }
            private set
            {
                this.m_Packet = value;
            }
        }

        public override long Position
        {
            get
            {
                return (long) this.m_Position;
            }
            set
            {
                if ((value < 0L) || (value >= this.m_Packet.Count))
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                this.m_Position = (int) value;
            }
        }
    }
}


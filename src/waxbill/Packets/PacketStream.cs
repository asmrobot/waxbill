using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics.Contracts;

namespace waxbill.Packets
{
    public class PacketStream:Stream
    {
        private Int32 m_Position;
        private IPacket m_Packet;
        public IPacket Packet
        {
            get
            {
                return m_Packet;
            }
            private set
            {
                m_Packet = value;
            }
        }


        public PacketStream(IPacket packet)
        {
            this.m_Packet = packet;
            m_Position = 0;
        }


        #region stream
        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {}

        public override long Length
        {
            get
            {
                return this.m_Packet.Count;
            }
        }

        public override long Position
        {
            get
            {
                return this.m_Position;
            }
            set
            {
                if (value<0||value >= this.m_Packet.Count)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                this.m_Position = (Int32)value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            if (offset < 0) throw new ArgumentOutOfRangeException("offset");
            if (offset + count > buffer.Length) throw new ArgumentOutOfRangeException("offset+count ");

            if (this.m_Position >= this.m_Packet.Count)
            {
                return 0;
            }
            int readlen= this.m_Packet.Read(this.m_Position, buffer, offset, count);
            this.m_Position += readlen;
            return readlen;
        }

        public override long Seek(long offset, SeekOrigin loc)
        {
            switch (loc)
            {
                case SeekOrigin.Begin:
                    {
                        
                        if (offset < 0 || offset>=this.m_Packet.Count)
                            throw new IOException("seek不在范围内");
                        this.m_Position = (Int32)offset;
                        break;
                    }
                case SeekOrigin.Current:
                    {
                        int tempPosition = unchecked(m_Position + (int)offset);
                        if (tempPosition > this.m_Packet.Count)
                            throw new IOException("seek 太大");
                        m_Position = tempPosition;
                        break;
                    }
                case SeekOrigin.End:
                    {
                        int tempPosition = unchecked((Int32)(this.m_Packet.Count + offset));
                        if (tempPosition < 0 || tempPosition >= this.m_Packet.Count)
                            throw new IOException("seek不在范围内");
                        this.m_Position = tempPosition;
                        break;
                    }
                default:
                    throw new ArgumentException("Argument_InvalidSeekOrigin");
            }

            Contract.Assert(this.m_Position >= 0, "_position >= 0");
            return m_Position;

        }

        public override void SetLength(long value)
        {}

        public override void Write(byte[] buffer, int offset, int count)
        {}
        #endregion

    }
}

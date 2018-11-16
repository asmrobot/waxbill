namespace ZTImage.Net.WebSockets
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Threading;
    using ZTImage.Net;

    internal class WaitHandleStream : Stream
    {
        private CancellationToken m_CancelToken;
        private MemoryStream m_InputStream = new MemoryStream();
        private int m_Position;
        private ConcurrentQueue<byte[]> m_Queue = new ConcurrentQueue<byte[]>();
        private bool m_Readable = true;
        private SocketSession m_Session;
        private ManualResetEvent m_WaitHandle = new ManualResetEvent(false);
        private bool m_Writeable = true;

        public WaitHandleStream(SocketSession session, CancellationToken cancelToken)
        {
            if (session == null)
            {
                throw new ArgumentNullException("session");
            }
            this.m_Session = session;
            this.m_CancelToken = cancelToken;
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
            while (!this.m_CancelToken.IsCancellationRequested)
            {
                if (this.m_Queue.IsEmpty)
                {
                    this.m_WaitHandle.Reset();
                    this.m_WaitHandle.WaitOne(0x3e8);
                }
                else
                {
                    byte[] buffer2;
                    int num = 0;
                    while (this.m_Queue.TryPeek(out buffer2))
                    {
                        int num2 = Math.Min((int) (buffer2.Length - this.m_Position), (int) (count - num));
                        Buffer.BlockCopy(buffer2, this.m_Position, buffer, offset + num, num2);
                        num += num2;
                        if ((this.m_Position + num2) < buffer2.Length)
                        {
                            this.m_Position += num2;
                            return num;
                        }
                        this.m_Queue.TryDequeue(out buffer2);
                        this.m_Position = 0;
                        if (num == count)
                        {
                            return num;
                        }
                    }
                    if (num > 0)
                    {
                        return num;
                    }
                }
            }
            return -1;
        }

        public override long Seek(long offset, SeekOrigin loc)
        {
            throw new NotSupportedException("不能Seek");
        }

        internal void SetData(byte[] buffer)
        {
            if ((buffer != null) && (buffer.Length >= 0))
            {
                this.m_Queue.Enqueue(buffer);
                this.m_WaitHandle.Set();
            }
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("不能设置长度");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.m_Session.Send(buffer, offset, count);
        }

        public override bool CanRead
        {
            get
            {
                return this.m_Readable;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return this.m_Writeable;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException("不能读取长度");
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException("不能读取位置");
            }
            set
            {
                throw new NotSupportedException("不能设置流位置");
            }
        }
    }
}


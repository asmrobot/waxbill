using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using waxbill.Sessions;

namespace waxbill.WebSockets
{

    internal class WaitHandleStream : Stream
    {
        private SessionBase session;
        private bool m_Readable = true;
        private bool m_Writeable = true;
        private ManualResetEvent m_WaitHandle = new ManualResetEvent(false);
        private MemoryStream m_InputStream = new MemoryStream();
        private ConcurrentQueue<byte[]> m_Queue = new ConcurrentQueue<byte[]>();
        private Int32 m_Position = 0;
        private CancellationToken m_CancelToken;
        

        public WaitHandleStream(SessionBase session,CancellationToken cancelToken)
        {
            if (session == null)
            {
                throw new ArgumentNullException("session");
            }
            this.session = session;
            this.m_CancelToken = cancelToken;
        }

        #region stream
        public override bool CanRead
        {
            get
            {
                return this.m_Readable;
            }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get
            {
                return this.m_Writeable;
            }
        }

        public override void Flush()
        { }

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

        public override void SetLength(long value)
        {
            throw new NotSupportedException("不能设置长度");
        }

        public override long Seek(long offset, SeekOrigin loc)
        {
            throw new NotSupportedException("不能Seek");
        }
        
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            if (offset < 0) throw new ArgumentOutOfRangeException("offset");
            if (offset + count > buffer.Length) throw new ArgumentOutOfRangeException("offset+count ");

            while (!this.m_CancelToken.IsCancellationRequested)
            {
                if (this.m_Queue.IsEmpty)
                {
                    m_WaitHandle.Reset();
                    m_WaitHandle.WaitOne(1000);
                    continue;
                }

                byte[] datas;
                int copyed = 0;
                while (this.m_Queue.TryPeek(out datas))
                {
                    int cc = Math.Min(datas.Length-this.m_Position, count-copyed);
                    Buffer.BlockCopy(datas, this.m_Position, buffer, offset+copyed, cc);
                    copyed += cc;
                    if (this.m_Position + cc < datas.Length)
                    {
                        //当复制的数据小于当前队列首的数据长度时,终止
                        this.m_Position += cc;
                        return copyed;
                    }
                    else
                    {
                        this.m_Queue.TryDequeue(out datas);
                        this.m_Position = 0;
                        
                        if (copyed == count)
                        {
                            //达到数量退出
                            return copyed;
                        }
                    }
                }

                if (copyed <= 0)
                {
                    continue;
                }
                return copyed;
            }

            return -1;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.session.Send(buffer, offset, count);
        }
        #endregion
        
        internal void SetData(byte[] buffer)
        {
            if (buffer == null || buffer.Length < 0)
            {
                return;
            }
            this.m_Queue.Enqueue(buffer);
            m_WaitHandle.Set();
        }
    }
}

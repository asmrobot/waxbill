using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using waxbill.Utils;

namespace waxbill.WebSockets
{
    public abstract class WebSocketSession:SocketSession
    {
        private const string magic = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        public readonly X509Certificate2 m_ServerCertificate;
        private WaitHandleStream m_InnerStream;
        private Stream m_Stream;
        private bool m_IsNegotiationThreadStart = false;
        private bool m_IsHandshake = false;
        private CancellationTokenSource m_CancelTokenSource;
        private bool m_IsSSL = false;

        public WebSocketSession(bool isssl,string  cerDir,string password)
        {
            this.m_IsSSL = isssl;
            if (this.m_IsSSL)
            {
                if (string.IsNullOrEmpty(cerDir))
                {
                    throw new ArgumentNullException("cerdir");
                }
                if (!File.Exists(cerDir))
                {
                    throw new ArgumentOutOfRangeException("cerdir not exists");
                }

                m_ServerCertificate = new X509Certificate2(cerDir, password);
            }
            m_CancelTokenSource = new CancellationTokenSource();            
            m_InnerStream = new WaitHandleStream(this,this.m_CancelTokenSource.Token);
            if (this.m_IsSSL)
            {
                m_Stream = new SslStream(m_InnerStream, false);
            }
            else
            {
                m_Stream = this.m_InnerStream;
            }
            
        }

        public WebSocketSession():this(false,string.Empty,string.Empty)
        { }

        protected override void ConnectedCallback()
        { }

        protected override void SendedCallback(SendingQueue packet, bool result)
        { }
        
        protected override void ReceiveCallback(Packet packet)
        {
            byte[] datas = packet.Read();
            m_InnerStream.SetData(datas);

            if (!m_IsNegotiationThreadStart)
            {
                m_IsNegotiationThreadStart = true;
                ThreadPool.QueueUserWorkItem(SessionThread);
            }
            return;
        }


        protected override void DisconnectedCallback(CloseReason reason)
        {
            
        }


        private void DoCancel()
        {
            this.m_CancelTokenSource.Cancel();
        }

        /// <summary>
        /// 会话线程
        /// </summary>
        /// <param name="item"></param>
        private void SessionThread(object item)
        {
            try
            {
                //ssl握手和websocket握手
                if (this.m_IsSSL)
                {
                    SslStream sslStream = m_Stream as SslStream;
                    if (sslStream == null)
                    {
                        throw new Exception("sslstream is error");
                    }
                    sslStream.AuthenticateAsServer(m_ServerCertificate, false, SslProtocols.Tls, true);
                }
                
                if (!m_IsHandshake)
                {
                    if (!handshake(m_Stream))
                    {
                        this.Close(CloseReason.ProtocolError);
                        return;
                    }
                }

                //正式接收数据
                while (!this.m_CancelTokenSource.IsCancellationRequested)
                {
                    WebSocketFrame frame = null;
                    try
                    {
                        frame = WebSocketFrame.GetFrameFromStream(m_Stream);
                    }
                    catch
                    {
                        continue;
                    }
                    if (frame == null)
                    {
                        continue;
                    }

                    try
                    {
                        ReceiveFrameCallback(frame);
                    }
                    catch (Exception ex)
                    {
                        //todo:log Trace.Error("frame处理失败", ex);
                        continue;
                    }
                    
                    //if (frame.IsString)
                    //{
                    //    string content = Encoding.UTF8.GetString(frame.Payload_data.Array, frame.Payload_data.Offset, frame.Payload_data.Count);
                    //    Console.WriteLine("接收数据:" + content);
                    //    SendFrame("is ok all data is receive");
                    //    return;
                    //}
                    //else
                    //{
                    //    Trace.Error("收到未知消息" + frame.ToString());
                    //}
                }
            }
            catch (Exception ex)
            {
                //todo:log Trace.Error("websocket ssion 中的未知错误", ex);
            }
            this.Close(CloseReason.Exception);
        }

        /// <summary>
        /// 从流中读取字符串
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private static string ReadMessage(Stream stream)
        {
            byte[] buffer = new byte[2048];
            int bytes = stream.Read(buffer, 0, buffer.Length);
            if (bytes > 0)
            {
                return Encoding.UTF8.GetString(buffer, 0, bytes);
            }
            return string.Empty;
        }

        /// <summary>
        /// websocket会话握手
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private bool handshake(Stream stream)
        {
            string content = string.Empty;
            while (true)
            {
                string message = ReadMessage(m_Stream);
                content += message;
                if (content.IndexOf("\r\n\r\n") > 0)
                {
                    break;
                }
            }

            if (string.IsNullOrEmpty(content))
            {
                this.Close(CloseReason.ProtocolError);
                return false;
            }
            string[] headers = content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (headers.Length < 4)
            {
                this.Close(CloseReason.ProtocolError);
                return false;
            }
            Dictionary<string, string> header_dic = new Dictionary<string, string>();
            for (int i = 1; i < headers.Length; i++)
            {
                string[] items = headers[i].Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (items.Length != 2)
                {
                    continue;
                }
                if (!header_dic.ContainsKey(items[0]))
                {
                    header_dic.Add(items[0], items[1]);
                }
            }

            if (!header_dic.ContainsKey("Connection") || !header_dic.ContainsKey("Upgrade"))
            {
                //todo:log Trace.Error("据手不包含Connections");
                this.Close(CloseReason.ProtocolError);
                return false;
            }

            if (!header_dic.ContainsKey("Sec-WebSocket-Key"))
            {
                //todo:log Trace.Error("据手不包含KEY");
                this.Close(CloseReason.ProtocolError);
                return false;
            }

            string fromkey = header_dic["Sec-WebSocket-Key"].Trim();
            string key = fromkey + magic;
            byte[] StrRes = Encoding.Default.GetBytes(key);
            HashAlgorithm iSHA = new SHA1CryptoServiceProvider();
            byte[] edatas = iSHA.ComputeHash(StrRes);

            string accept = GetBase64(edatas);
            string model = "HTTP/1.1 101 Switching Protocols\r\nUpgrade:websocket\r\nConnection:Upgrade\r\nSec-WebSocket-Accept:" + accept + "\r\n\r\n";

            byte[] sends = Encoding.UTF8.GetBytes(model);
            stream.Write(sends, 0, sends.Length);
            m_IsHandshake = true;
            //Trace.Info("已处理握手");
            HandshakeCallback();
            return true;
        }


        private string GetBase64(byte[] data)
        {
            if (data == null || data.Length <= 0)
            {
                return string.Empty;
            }

            return Convert.ToBase64String(data);
        }
                
        /// <summary>
        /// websocket发送数据
        /// </summary>
        /// <param name="content"></param>
        protected void SendFrame(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return;
            }

            byte[] _StringDatas = System.Text.Encoding.UTF8.GetBytes(content);
            byte[] _SendDatas = WebSocketFrame.GetDatas(1, Opcode.TextFrame, new ArraySegment<byte>(_StringDatas, 0, _StringDatas.Length));
            this.m_Stream.Write(_SendDatas,0,_SendDatas.Length);
        }

        /// <summary>
        /// websocket发送数据
        /// </summary>
        /// <param name="datas"></param>
        protected void SendFrame(byte[] datas)
        {
            if (datas == null || datas.Length <= 0)
            {
                return;
            }
            byte[] _SendDatas = WebSocketFrame.GetDatas(1, Opcode.TextFrame, new ArraySegment<byte>(datas, 0, datas.Length));
            this.m_Stream.Write(_SendDatas,0,_SendDatas.Length);
        }
        
        /// <summary>
        /// 打印信息
        /// </summary>
        /// <param name="stream"></param>
        private static void DisplaySecurityLevel(SslStream stream)
        {
            //security level
            Console.WriteLine("Cipher: {0} strength {1}", stream.CipherAlgorithm, stream.CipherStrength);
            Console.WriteLine("Hash: {0} strength {1}", stream.HashAlgorithm, stream.HashStrength);
            Console.WriteLine("Key exchange: {0} strength {1}", stream.KeyExchangeAlgorithm, stream.KeyExchangeStrength);
            Console.WriteLine("Protocol: {0}", stream.SslProtocol);
            //security services
            Console.WriteLine("Is authenticated: {0} as server? {1}", stream.IsAuthenticated, stream.IsServer);
            Console.WriteLine("IsSigned: {0}", stream.IsSigned);
            Console.WriteLine("Is Encrypted: {0}", stream.IsEncrypted);

            //stream properties
            Console.WriteLine("Can read: {0}, write {1}", stream.CanRead, stream.CanWrite);
            Console.WriteLine("Can timeout: {0}", stream.CanTimeout);

            //certification information
            Console.WriteLine("Certificate revocation list checked: {0}", stream.CheckCertRevocationStatus);

            X509Certificate localCertificate = stream.LocalCertificate;
            if (stream.LocalCertificate != null)
            {
                Console.WriteLine("Local cert was issued to {0} and is valid from {1} until {2}.",
                localCertificate.Subject,
                    localCertificate.GetEffectiveDateString(),
                    localCertificate.GetExpirationDateString());
            }
            else
            {
                Console.WriteLine("Local certificate is null.");
            }
            X509Certificate remoteCertificate = stream.RemoteCertificate;
            if (stream.RemoteCertificate != null)
            {
                Console.WriteLine("Remote cert was issued to {0} and is valid from {1} until {2}.",
                    remoteCertificate.Subject,
                    remoteCertificate.GetEffectiveDateString(),
                    remoteCertificate.GetExpirationDateString());
            }
            else
            {
                Console.WriteLine("Remote certificate is null.");
            }
        }
        

        #region abstruct
        /// <summary>
        /// websocket握手成功回调
        /// </summary>
        protected abstract void HandshakeCallback();

        /// <summary>
        /// websocket接收帧回调
        /// </summary>
        /// <param name="frame"></param>
        protected abstract void ReceiveFrameCallback(WebSocketFrame frame);

        /// <summary>
        /// websocket断开时
        /// </summary>
        protected abstract void WebSocketCloseCallback();
        #endregion
    }

}

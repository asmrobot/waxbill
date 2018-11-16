namespace ZTImage.Net.WebSockets
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Security;
    using System.Security.Authentication;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using ZTImage.Log;
    using ZTImage.Net;
    using ZTImage.Net.Utils;
    using ZTImage.Text;

    public abstract class WebSocketSession : SocketSession
    {
        private CancellationTokenSource m_CancelTokenSource;
        private WaitHandleStream m_InnerStream;
        private bool m_IsHandshake;
        private bool m_IsNegotiationThreadStart;
        private bool m_IsSSL;
        public readonly X509Certificate2 m_ServerCertificate;
        private Stream m_Stream;
        private const string magic = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        public WebSocketSession() : this(false, string.Empty, string.Empty)
        {
        }

        public WebSocketSession(bool isssl, string cerDir, string password)
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
                this.m_ServerCertificate = new X509Certificate2(cerDir, password);
            }
            this.m_CancelTokenSource = new CancellationTokenSource();
            this.m_InnerStream = new WaitHandleStream(this, this.m_CancelTokenSource.Token);
            if (this.m_IsSSL)
            {
                this.m_Stream = new SslStream(this.m_InnerStream, false);
            }
            else
            {
                this.m_Stream = this.m_InnerStream;
            }
        }

        protected override void ConnectedCallback()
        {
        }

        protected override void DisconnectedCallback(CloseReason reason)
        {
        }

        private static void DisplaySecurityLevel(SslStream stream)
        {
            Console.WriteLine("Cipher: {0} strength {1}", stream.CipherAlgorithm, stream.CipherStrength);
            Console.WriteLine("Hash: {0} strength {1}", stream.HashAlgorithm, stream.HashStrength);
            Console.WriteLine("Key exchange: {0} strength {1}", stream.KeyExchangeAlgorithm, stream.KeyExchangeStrength);
            Console.WriteLine("Protocol: {0}", stream.SslProtocol);
            Console.WriteLine("Is authenticated: {0} as server? {1}", stream.IsAuthenticated, stream.IsServer);
            Console.WriteLine("IsSigned: {0}", stream.IsSigned);
            Console.WriteLine("Is Encrypted: {0}", stream.IsEncrypted);
            Console.WriteLine("Can read: {0}, write {1}", stream.CanRead, stream.CanWrite);
            Console.WriteLine("Can timeout: {0}", stream.CanTimeout);
            Console.WriteLine("Certificate revocation list checked: {0}", stream.CheckCertRevocationStatus);
            X509Certificate localCertificate = stream.LocalCertificate;
            if (stream.LocalCertificate != null)
            {
                Console.WriteLine("Local cert was issued to {0} and is valid from {1} until {2}.", localCertificate.Subject, localCertificate.GetEffectiveDateString(), localCertificate.GetExpirationDateString());
            }
            else
            {
                Console.WriteLine("Local certificate is null.");
            }
            X509Certificate remoteCertificate = stream.RemoteCertificate;
            if (stream.RemoteCertificate != null)
            {
                Console.WriteLine("Remote cert was issued to {0} and is valid from {1} until {2}.", remoteCertificate.Subject, remoteCertificate.GetEffectiveDateString(), remoteCertificate.GetExpirationDateString());
            }
            else
            {
                Console.WriteLine("Remote certificate is null.");
            }
        }

        private void DoCancel()
        {
            this.m_CancelTokenSource.Cancel();
        }

        private bool handshake(Stream stream)
        {
            string str = string.Empty;
            do
            {
                string str5 = ReadMessage(this.m_Stream);
                str = str + str5;
            }
            while (str.IndexOf("\r\n\r\n") <= 0);
            if (string.IsNullOrEmpty(str))
            {
                base.Close(CloseReason.ProtocolError);
                return false;
            }
            string[] separator = new string[] { "\r\n" };
            string[] strArray = str.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            if (strArray.Length < 4)
            {
                base.Close(CloseReason.ProtocolError);
                return false;
            }
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            for (int i = 1; i < strArray.Length; i++)
            {
                char[] chArray1 = new char[] { ':' };
                string[] strArray2 = strArray[i].Split(chArray1, StringSplitOptions.RemoveEmptyEntries);
                if ((strArray2.Length == 2) && !dictionary.ContainsKey(strArray2[0]))
                {
                    dictionary.Add(strArray2[0], strArray2[1]);
                }
            }
            if (!dictionary.ContainsKey("Connection") || !dictionary.ContainsKey("Upgrade"))
            {
                Trace.Error("据手不包含Connections");
                base.Close(CloseReason.ProtocolError);
                return false;
            }
            if (!dictionary.ContainsKey("Sec-WebSocket-Key"))
            {
                Trace.Error("据手不包含KEY");
                base.Close(CloseReason.ProtocolError);
                return false;
            }
            string s = dictionary["Sec-WebSocket-Key"].Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            byte[] bytes = Encoding.Default.GetBytes(s);
            string str3 = Coding.EncodeBase64(new SHA1CryptoServiceProvider().ComputeHash(bytes));
            string str4 = "HTTP/1.1 101 Switching Protocols\r\nUpgrade:websocket\r\nConnection:Upgrade\r\nSec-WebSocket-Accept:" + str3 + "\r\n\r\n";
            byte[] buffer = Encoding.UTF8.GetBytes(str4);
            stream.Write(buffer, 0, buffer.Length);
            this.m_IsHandshake = true;
            this.HandshakeCallback();
            return true;
        }

        protected abstract void HandshakeCallback();
        private static string ReadMessage(Stream stream)
        {
            byte[] buffer = new byte[0x800];
            int count = stream.Read(buffer, 0, buffer.Length);
            if (count > 0)
            {
                return Encoding.UTF8.GetString(buffer, 0, count);
            }
            return string.Empty;
        }

        protected override void ReceiveCallback(Packet packet)
        {
            byte[] buffer = packet.Read();
            this.m_InnerStream.SetData(buffer);
            if (!this.m_IsNegotiationThreadStart)
            {
                this.m_IsNegotiationThreadStart = true;
                ThreadPool.QueueUserWorkItem(new WaitCallback(this.SessionThread));
            }
        }

        protected abstract void ReceiveFrameCallback(WebSocketFrame frame);
        protected override void SendedCallback(SendingQueue packet, bool result)
        {
        }

        public void SendFrame(string content)
        {
            if (!string.IsNullOrEmpty(content))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(content);
                byte[] buffer = WebSocketFrame.GetDatas(1, Opcode.TextFrame, new ArraySegment<byte>(bytes, 0, bytes.Length));
                this.m_Stream.Write(buffer, 0, buffer.Length);
            }
        }

        public void SendFrame(byte[] datas)
        {
            if ((datas != null) && (datas.Length != 0))
            {
                byte[] buffer = WebSocketFrame.GetDatas(1, Opcode.TextFrame, new ArraySegment<byte>(datas, 0, datas.Length));
                this.m_Stream.Write(buffer, 0, buffer.Length);
            }
        }

        private void SessionThread(object item)
        {
            try
            {
                WebSocketFrame frameFromStream;
                if (this.m_IsSSL)
                {
                    SslStream stream = this.m_Stream as SslStream;
                    if (stream == null)
                    {
                        throw new Exception("sslstream is error");
                    }
                    stream.AuthenticateAsServer(this.m_ServerCertificate, false, SslProtocols.Tls, true);
                }
                if (this.m_IsHandshake || this.handshake(this.m_Stream))
                {
                    goto Label_0080;
                }
                base.Close(CloseReason.ProtocolError);
                return;
            Label_0052:
                frameFromStream = null;
                try
                {
                    frameFromStream = WebSocketFrame.GetFrameFromStream(this.m_Stream);
                }
                catch
                {
                    goto Label_0080;
                }
                if (frameFromStream != null)
                {
                    try
                    {
                        this.ReceiveFrameCallback(frameFromStream);
                    }
                    catch (Exception exception)
                    {
                        Trace.Error("frame处理失败", exception);
                    }
                }
            Label_0080:
                if (!this.m_CancelTokenSource.IsCancellationRequested)
                {
                    goto Label_0052;
                }
            }
            catch (Exception exception2)
            {
                Trace.Error("websocket ssion 中的未知错误", exception2);
            }
            base.Close(CloseReason.Exception);
        }

        protected abstract void WebSocketCloseCallback();
    }
}


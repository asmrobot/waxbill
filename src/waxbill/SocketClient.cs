using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using ZTImage.Log;
using ZTImage.Net.Exceptions;

namespace waxbill
{
    

    public class SocketClient
    {
        private static byte[] combinDatas(IList<ArraySegment<byte>> datas)
        {
            return new byte[0];
        }

        public static void Send(string ip, int port, byte[] datas)
        {
            Send(ip, port, datas, 0, datas.Length, <>c.<>9__0_0 ?? (<>c.<>9__0_0 = new Action<Socket>(<>c.<>9.<Send>b__0_0)));
        }

        public static void Send(string ip, int port, ArraySegment<byte> data)
        {
            Send(ip, port, data.Array, data.Offset, data.Count, <>c.<>9__2_0 ?? (<>c.<>9__2_0 = new Action<Socket>(<>c.<>9.<Send>b__2_0)));
        }

        public static void Send(string ip, int port, IList<ArraySegment<byte>> datas)
        {
            byte[] buffer = combinDatas(datas);
            Send(ip, port, buffer, 0, buffer.Length, <>c.<>9__3_0 ?? (<>c.<>9__3_0 = new Action<Socket>(<>c.<>9.<Send>b__3_0)));
        }

        public static void Send(string ip, int port, byte[] datas, Action<Socket> receiveAction)
        {
            Send(ip, port, datas, 0, datas.Length, receiveAction);
        }

        public static void Send(string ip, int port, ArraySegment<byte> data, Action<Socket> receiveAction)
        {
            Send(ip, port, data.Array, data.Offset, data.Count, receiveAction);
        }

        public static void Send(string ip, int port, IList<ArraySegment<byte>> datas, Action<Socket> receiveAction)
        {
            byte[] buffer = combinDatas(datas);
            Send(ip, port, buffer, 0, buffer.Length, receiveAction);
        }

        public static void Send(string ip, int port, byte[] datas, int offset, int size)
        {
            Send(ip, port, datas, offset, size, <>c.<>9__1_0 ?? (<>c.<>9__1_0 = new Action<Socket>(<>c.<>9.<Send>b__1_0)));
        }

        public static void Send(string ip, int port, byte[] datas, int offset, int size, Action<Socket> receiveAction)
        {
            if (((offset < 0) || (size < 0)) || (datas.Length < (offset + size)))
            {
                throw new ArgumentOutOfRangeException("发送数据大小不合理");
            }
            IPAddress address = null;
            if (!IPAddress.TryParse(ip, out address))
            {
                throw new IPParseException();
            }
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(new IPEndPoint(address, port));
                socket.Send(datas, offset, size, SocketFlags.None);
            }
            catch (Exception exception)
            {
                Trace.Error("发送数据时出错", exception);
                throw new NoNetException();
            }
            if (receiveAction != null)
            {
                try
                {
                    socket.ReceiveTimeout = 0x1388;
                    receiveAction(socket);
                }
                catch (Exception exception2)
                {
                    Trace.Error("调用接收函数出错", exception2);
                    throw new CustomReceiveException();
                }
            }
            try
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch
            {
            }
        }

        [Serializable, CompilerGenerated]
        private sealed class <>c
        {
            public static readonly SocketClient.<>c <>9 = new SocketClient.<>c();
            public static Action<Socket> <>9__0_0;
            public static Action<Socket> <>9__1_0;
            public static Action<Socket> <>9__2_0;
            public static Action<Socket> <>9__3_0;

            internal void <Send>b__0_0(Socket socket)
            {
            }

            internal void <Send>b__1_0(Socket socket)
            {
            }

            internal void <Send>b__2_0(Socket socket)
            {
            }

            internal void <Send>b__3_0(Socket socket)
            {
            }
        }
    }
}


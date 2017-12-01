﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using waxbill.Libuv.Collections;

namespace waxbill.Libuv
{
    //reference from kestrel


    /// <summary>
    /// libuv introp 
    /// </summary>
    public class UVIntrop
    {
        public static readonly bool IsWindows;
        
        static UVIntrop()
        {
            IsWindows = System.Environment.OSVersion.Platform == PlatformID.Win32NT;
        }

        #region tools
        public static void ThrowIfErrored(int statusCode)
        {
            // Note: method is explicitly small so the success case is easily inlined
            if (statusCode < 0)
            {
                ThrowError(statusCode);
            }
        }

        private static void ThrowError(int statusCode)
        {
            // Note: only has one throw block so it will marked as "Does not return" by the jit
            // and not inlined into previous function, while also marking as a function
            // that does not need cpu register prep to call (see: https://github.com/dotnet/coreclr/pull/6103)
            throw GetError(statusCode);
        }

        public static void Check(int statusCode, out UVException error)
        {
            // Note: method is explicitly small so the success case is easily inlined
            error = statusCode < 0 ? GetError(statusCode) : null;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static UVException GetError(int statusCode)
        {
            // Note: method marked as NoInlining so it doesn't bloat either of the two preceeding functions
            // Check and ThrowError and alter their jit heuristics.
            var errorName =err_name(statusCode);
            var errorDescription =strerror(statusCode);
            return new UVException("Error " + statusCode + " " + errorName + " " + errorDescription, statusCode);
        }
        #endregion

        #region struct
        public enum RequestType
        {
            Unknown = 0,
            REQ,
            CONNECT,
            WRITE,
            SHUTDOWN,
            UDP_SEND,
            FS,
            WORK,
            GETADDRINFO,
            GETNAMEINFO,
        }

        public enum HandleType
        {
            Unknown = 0,
            ASYNC,
            CHECK,
            FS_EVENT,
            FS_POLL,
            HANDLE,
            IDLE,
            NAMED_PIPE,
            POLL,
            PREPARE,
            PROCESS,
            STREAM,
            TCP,
            TIMER,
            TTY,
            UDP,
            SIGNAL,
        }


        public enum UV_RUN_MODE:Int32
        {
            UV_RUN_DEFAULT = 0,
            UV_RUN_ONCE,
            UV_RUN_NOWAIT
        }

        public struct uv_buf_t
        {
            // this type represents a WSABUF struct on Windows
            // https://msdn.microsoft.com/en-us/library/windows/desktop/ms741542(v=vs.85).aspx
            // and an iovec struct on *nix
            // http://man7.org/linux/man-pages/man2/readv.2.html
            // because the order of the fields in these structs is different, the field
            // names in this type don't have meaningful symbolic names. instead, they are
            // assigned in the correct order by the constructor at runtime

            internal readonly IntPtr _field0;
            internal readonly IntPtr _field1;

            public uv_buf_t(IntPtr memory, int len, bool IsWindows)
            {
                if (IsWindows)
                {
                    _field0 = (IntPtr)len;
                    _field1 = memory;
                }
                else
                {
                    _field0 = memory;
                    _field1 = (IntPtr)len;
                }
            }
        }
        #endregion


        #region Simplify Call

        public static void loop_init(UVLoopHandle handle)
        {
            ThrowIfErrored(uv_loop_init(handle));
        }

        public static void loop_close(UVLoopHandle handle)
        {
            handle.Validate(closed: true);
            ThrowIfErrored(uv_loop_close(handle.InternalGetHandle()));
        }

        public static void run(UVLoopHandle handle, int mode)
        {
            handle.Validate();
            ThrowIfErrored(uv_run(handle, mode));
        }

        public static void stop(UVLoopHandle handle)
        {
            handle.Validate();
            uv_stop(handle);
        }

        public static void @ref(UVHandle handle)
        {
            handle.Validate();
            uv_ref(handle);
        }

        public static void unref(UVHandle handle)
        {
            handle.Validate();
            uv_unref(handle);
        }

        public static void fileno(UVHandle handle, ref IntPtr socket)
        {
            handle.Validate();
            ThrowIfErrored(uv_fileno(handle, ref socket));
        }
        
        public static void close(UVHandle handle, uv_close_cb close_cb)
        {
            handle.Validate(closed: true);
            uv_close(handle.InternalGetHandle(), close_cb);
        }

        public static void close(IntPtr handle, uv_close_cb close_cb)
        {
            uv_close(handle, close_cb);
        }

        public static void async_init(UVLoopHandle loop, UVAsyncHandle handle, uv_async_cb cb)
        {
            loop.Validate();
            handle.Validate();
            ThrowIfErrored(uv_async_init(loop, handle, cb));
        }

        public static void async_send(UVAsyncHandle handle)
        {
            ThrowIfErrored(uv_async_send(handle));
        }

        public static void unsafe_async_send(IntPtr handle)
        {
            ThrowIfErrored(uv_unsafe_async_send(handle));
        }

        public static void tcp_init(UVLoopHandle loop, UVTCPHandle handle)
        {
            loop.Validate();
            handle.Validate();
            ThrowIfErrored(uv_tcp_init(loop, handle));
        }

        public static void tcp_bind(UVTCPHandle handle, ref SockAddr addr, int flags)
        {
            handle.Validate();
            ThrowIfErrored(uv_tcp_bind(handle, ref addr, flags));
        }

        public static void tcp_open(UVTCPHandle handle, IntPtr hSocket)
        {
            handle.Validate();
            ThrowIfErrored(uv_tcp_open(handle, hSocket));
        }

        public static void tcp_nodelay(UVTCPHandle handle, bool enable)
        {
            handle.Validate();
            ThrowIfErrored(uv_tcp_nodelay(handle, enable ? 1 : 0));
        }

        public static void pipe_init(UVLoopHandle loop, UVPipeHandle handle, bool ipc)
        {
            loop.Validate();
            handle.Validate();
            ThrowIfErrored(uv_pipe_init(loop, handle, ipc ? -1 : 0));
        }

        public static void pipe_bind(UVPipeHandle handle, string name)
        {
            handle.Validate();
            ThrowIfErrored(uv_pipe_bind(handle, name));
        }

        public static void pipe_open(UVPipeHandle handle, IntPtr hSocket)
        {
            handle.Validate();
            ThrowIfErrored(uv_pipe_open(handle, hSocket));
        }

        public static void listen(UVStreamHandle handle, int backlog, uv_connection_cb cb)
        {
            handle.Validate();
            ThrowIfErrored(uv_listen(handle, backlog, cb));
        }

        public static void accept(UVStreamHandle server, UVStreamHandle client)
        {
            server.Validate();
            client.Validate();
            ThrowIfErrored(uv_accept(server, client));
        }

        public static void pipe_connect(UVConnectRequest req, UVPipeHandle handle, string name, uv_connect_cb cb)
        {
            req.Validate();
            handle.Validate();
            uv_pipe_connect(req, handle, name, cb);
        }

        public static int pipe_pending_count(UVPipeHandle handle)
        {
            handle.Validate();
            return uv_pipe_pending_count(handle);
        }

        public static void read_start(UVStreamHandle handle, uv_alloc_cb alloc_cb, uv_read_cb read_cb)
        {
            handle.Validate();
            ThrowIfErrored(uv_read_start(handle, alloc_cb, read_cb));
        }

        public static void read_stop(UVStreamHandle handle)
        {
            handle.Validate();
            ThrowIfErrored(uv_read_stop(handle));
        }

        public static int try_write(UVStreamHandle handle, uv_buf_t[] bufs, int nbufs)
        {
            handle.Validate();
            var count = uv_try_write(handle, bufs, nbufs);
            ThrowIfErrored(count);
            return count;
        }
        
        unsafe public static void write(UVRequest req, UVStreamHandle handle, uv_buf_t* bufs, int nbufs, uv_write_cb cb)
        {
            req.Validate();
            handle.Validate();
            ThrowIfErrored(uv_write(req, handle, bufs, nbufs, cb));
        }

        unsafe public static void write2(UVRequest req, UVStreamHandle handle, uv_buf_t* bufs, int nbufs, UVStreamHandle sendHandle, uv_write_cb cb)
        {
            req.Validate();
            handle.Validate();
            ThrowIfErrored(uv_write2(req, handle, bufs, nbufs, sendHandle, cb));
        }

        public static string err_name(int err)
        {
            IntPtr ptr = uv_err_name(err);
            return ptr == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(ptr);
        }

        public static string strerror(int err)
        {
            IntPtr ptr = uv_strerror(err);
            return ptr == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(ptr);
        }

        public static int loop_size()
        {
            return uv_loop_size();
        }

        public static int handle_size(HandleType handleType)
        {
            return uv_handle_size(handleType);
        }

        public static int req_size(RequestType reqType)
        {
            return uv_req_size(reqType);
        }

        public static void ip4_addr(string ip, int port, out SockAddr addr, out UVException error)
        {
            Check(uv_ip4_addr(ip, port, out addr), out error);
        }

        public static void ip6_addr(string ip, int port, out SockAddr addr, out UVException error)
        {
            Check(uv_ip6_addr(ip, port, out addr), out error);
        }

        public static void walk(UVLoopHandle loop, uv_walk_cb walk_cb, IntPtr arg)
        {
            loop.Validate();
            uv_walk(loop, walk_cb, arg);
        }

        unsafe public static void timer_init(UVLoopHandle loop, UVTimerHandle handle)
        {
            loop.Validate();
            handle.Validate();
            ThrowIfErrored(uv_timer_init(loop, handle));
        }

        
        unsafe public static void timer_start(UVTimerHandle handle, uv_timer_cb cb, long timeout, long repeat)
        {
            handle.Validate();
            ThrowIfErrored(uv_timer_start(handle, cb, timeout, repeat));
        }

        
        unsafe public static void timer_stop(UVTimerHandle handle)
        {
            handle.Validate();
            ThrowIfErrored(uv_timer_stop(handle));
        }
        
        unsafe public static long now(UVLoopHandle loop)
        {
            loop.Validate();
            return uv_now(loop);
        }

        
        public static void tcp_getsockname(UVTCPHandle handle, out SockAddr addr, ref int namelen)
        {
            handle.Validate();
            ThrowIfErrored(uv_tcp_getsockname(handle, out addr, ref namelen));
        }

        
        public static void tcp_getpeername(UVTCPHandle handle, out SockAddr addr, ref int namelen)
        {
            handle.Validate();
            ThrowIfErrored(uv_tcp_getpeername(handle, out addr, ref namelen));
        }

        public static uv_buf_t buf_init(IntPtr memory, int len)
        {
            return new uv_buf_t(memory, len, IsWindows);
        }
        #endregion


        #region unmanaged delegate
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_connect_cb(IntPtr req, int status);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_connection_cb(IntPtr server, int status);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_close_cb(IntPtr handle);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_async_cb(IntPtr handle);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_alloc_cb(IntPtr server, int suggested_size, out uv_buf_t buf);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_read_cb(IntPtr server, int nread, ref uv_buf_t buf);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_write_cb(IntPtr req, int status);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_walk_cb(IntPtr handle, IntPtr arg);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void uv_timer_cb(IntPtr handle);
        #endregion

        #region declare
        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_loop_init(UVLoopHandle handle);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_loop_close(IntPtr a0);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_run(UVLoopHandle handle, int mode);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern void uv_stop(UVLoopHandle handle);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern void uv_ref(UVHandle handle);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern void uv_unref(UVHandle handle);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_fileno(UVHandle handle, ref IntPtr socket);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern void uv_close(IntPtr handle, uv_close_cb close_cb);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_async_init(UVLoopHandle loop, UVAsyncHandle handle, uv_async_cb cb);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public extern static int uv_async_send(UVAsyncHandle handle);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl, EntryPoint = "uv_async_send")]
        public extern static int uv_unsafe_async_send(IntPtr handle);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_tcp_init(UVLoopHandle loop, UVTCPHandle handle);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_tcp_bind(UVTCPHandle handle, ref SockAddr addr, int flags);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_tcp_open(UVTCPHandle handle, IntPtr hSocket);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_tcp_nodelay(UVTCPHandle handle, int enable);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_pipe_init(UVLoopHandle loop, UVPipeHandle handle, int ipc);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_pipe_bind(UVPipeHandle loop, string name);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_pipe_open(UVPipeHandle handle, IntPtr hSocket);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_listen(UVStreamHandle handle, int backlog, uv_connection_cb cb);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_accept(UVStreamHandle server, UVStreamHandle client);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void uv_pipe_connect(UVConnectRequest req, UVPipeHandle handle, string name, uv_connect_cb cb);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public extern static int uv_pipe_pending_count(UVPipeHandle handle);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public extern static int uv_read_start(UVStreamHandle handle, uv_alloc_cb alloc_cb, uv_read_cb read_cb);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_read_stop(UVStreamHandle handle);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_try_write(UVStreamHandle handle, uv_buf_t[] bufs, int nbufs);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        unsafe public static extern int uv_write(UVRequest req, UVStreamHandle handle, uv_buf_t* bufs, int nbufs, uv_write_cb cb);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        unsafe public static extern int uv_write2(UVRequest req, UVStreamHandle handle, uv_buf_t* bufs, int nbufs, UVStreamHandle sendHandle, uv_write_cb cb);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public extern static IntPtr uv_err_name(int err);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr uv_strerror(int err);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_loop_size();

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_handle_size(HandleType handleType);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_req_size(RequestType reqType);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_ip4_addr(string ip, int port, out SockAddr addr);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_ip6_addr(string ip, int port, out SockAddr addr);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_tcp_getsockname(UVTCPHandle handle, out SockAddr name, ref int namelen);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_tcp_getpeername(UVTCPHandle handle, out SockAddr name, ref int namelen);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_walk(UVLoopHandle loop, uv_walk_cb walk_cb, IntPtr arg);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        unsafe public static extern int uv_timer_init(UVLoopHandle loop, UVTimerHandle handle);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        unsafe public static extern int uv_timer_start(UVTimerHandle handle, uv_timer_cb cb, long timeout, long repeat);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        unsafe public static extern int uv_timer_stop(UVTimerHandle handle);

        [DllImport("libuv", CallingConvention = CallingConvention.Cdecl)]
        unsafe public static extern long uv_now(UVLoopHandle loop);

        [DllImport("WS2_32.dll", CallingConvention = CallingConvention.Winapi)]
        unsafe public static extern int WSAIoctl(
            IntPtr socket,
            int dwIoControlCode,
            int* lpvInBuffer,
            uint cbInBuffer,
            int* lpvOutBuffer,
            int cbOutBuffer,
            out uint lpcbBytesReturned,
            IntPtr lpOverlapped,
            IntPtr lpCompletionRoutine
        );

        [DllImport("WS2_32.dll", CallingConvention = CallingConvention.Winapi)]
        public static extern int WSAGetLastError();
        #endregion


    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Tunneling.Core
{
    public class TcpSession
    {
        public uint Id { get; private set; }
        public bool IsClosed { get; private set; } = false;
        public EndPoint RemoteHost { get; private set; }
        public Socket? Connection { get; private set; }
        public TcpServer? Server { get; private set; }
        public SocketAsyncEventArgs? SocketAsyncEventArgs { get; private set; }

        public TcpSession(uint Id, TcpServer server, Socket connection, SocketAsyncEventArgs asyncEventArgs)
        {
            asyncEventArgs.UserToken = this;

            this.Id = Id;
            this.SocketAsyncEventArgs = asyncEventArgs;
            this.Server = server;
            this.Connection = connection;
            this.RemoteHost = connection.RemoteEndPoint;
            //const int send_buffer_size = 2 * 1024 * 1024;
            //this.Connection.SendBufferSize = send_buffer_size;
        }

        public void Close()
        {
            if (IsClosed) return;
            IsClosed = true;

            try
            {
                this.Server?.CloseSession(this);

                try
                {
                    if (Connection.Connected) Connection.Shutdown(SocketShutdown.Both);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                Connection?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            this.Server = null;
            this.Connection = null;
            this.SocketAsyncEventArgs = null;
        }

        volatile private int _CheckHealth = 0;
        public void CheckHealth()
        {
            var count = Interlocked.Increment(ref _CheckHealth);
            if (count > 3)
            {
                //超过3次(1分钟)都没有收发数据，就认为是死连接，需要关闭
                Close();
            }
        }

        internal void ResetHealth()
        {
            Interlocked.Exchange(ref _CheckHealth, 0);
        }

        public async Task<bool> Send(byte[] data)
        {
            //const int MAX_PacketSize = 65536;
            bool ok = false;
            if (!IsClosed)
            {
                ResetHealth();

                int offset = 0;
                ReadOnlyMemory<byte> buff = data;

                while (offset < data.Length)
                {
                    //计算需要发送的数据量
                    var len = data.Length - offset;

                    //开始发送
                    int sent = await Connection.SendAsync(buff.Slice(offset, len), SocketFlags.None);
                    if (sent <= 0)
                    {
                        Close();
                        return ok;
                    }
                    offset += sent;
                }

                ok = true;
            }

            return ok;
        }
    }
}

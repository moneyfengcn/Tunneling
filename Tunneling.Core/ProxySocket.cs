using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Tunneling.Core
{
    public class ProxySocket(ProxyDispatcher taskDispatcher, string channelName, uint sessionId, string ip, int port)
    {
        public ProxyDispatcher TaskDispatcher { get; } = taskDispatcher;
        public uint SessionId { get; } = sessionId;
        public string ChannelName { get; } = channelName;
        public bool IsConnected { get; internal set; } = false;
        public string PrivateHost { get; } = ip;
        public int PrivatePort { get; } = port;

        private Socket? _client;
        public bool Connect()
        {
            _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                _client.Connect(ip, port);
                _client.ReceiveTimeout = 60 * 1000;
                //_client.ReceiveBufferSize = ushort.MaxValue;

                IsConnected = _client.Connected;

                Task.Factory.StartNew(DoTask);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return IsConnected;
        }
        public int FlowControl { get; set; } = 10;
        private int _sendCount = 0;
        async void DoTask()
        {
            const int BUFFER_SIZE = 16 * 1024;
            byte[] buffer = new byte[BUFFER_SIZE];
            while (!IsClosed)
            {
                try
                {
                    var bytesRead = await _client.ReceiveAsync(buffer);
                    if (bytesRead > 0)
                    {
                        byte[] receivedData = new byte[bytesRead];
                        Buffer.BlockCopy(buffer, 0, receivedData, 0, bytesRead);

                        await TaskDispatcher.OnDataReceived(this, _sendCount++, receivedData);
                        if (_sendCount > this.FlowControl) _sendCount = 0;
                    }
                    else
                    {
                        Disconnect();
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    break;
                }
            }
            Disconnect();
        }



        volatile private bool IsClosed = false;
        public void Disconnect()
        {
            IsConnected = false;
            if (!IsClosed)
            {
                IsClosed = true;

                if (_client != null)
                {
                    try
                    {
                        if (_client.Connected)
                        {
                            _client.Shutdown(SocketShutdown.Both);
                        }
                        _client.Dispose();
                        _client = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }
                this.TaskDispatcher.RaiseEvent_OnDisconnect(this.ChannelName, this.SessionId);
            }
        }

        async public Task<bool> SendData(byte[] data)
        {
            bool result = false;

            if (!IsClosed && _client != null)
            {
                try
                {
                    int offset = 0;
                    ReadOnlyMemory<byte> buff = data;

                    while (offset < data.Length)
                    {
                        var len = data.Length - offset;

                        //开始发送
                        int sent = await _client.SendAsync(buff.Slice(offset, len), SocketFlags.None);
                        if (sent <= 0)
                        {
                            Disconnect();
                            return false;
                        }
                        offset += sent;
                    }
                    result = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    Disconnect();
                }
            }

            return result;
        }
    }
}


using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Tunneling.Core
{
    public delegate Task DataReceivedEventHandler(ProxySocket session, int sendCount, byte[] data);
    public delegate void ProxyEventHandler(string channelName, uint sessionId, bool isConnected);

    public class ProxyDispatcher : IDisposable
    {
        public DataReceivedEventHandler? OnDataReceived;
        public event ProxyEventHandler OnProxyConnected;

        private ConcurrentDictionary<string, List<ProxySocket>> _proxy = new ConcurrentDictionary<string, List<ProxySocket>>();

        public bool IsClosed { get; private set; } = false;
        public int FlowControl { get; set; } = 10; //默认10 

        public void SyncConversationList(Action<string, List<uint>> callback)
        {
            try
            {
                var data = _proxy.ToList();
                foreach (var proxy in data)
                {
                    var lstSessionId = new List<uint>();
                    foreach (var item in proxy.Value)
                    {
                        lstSessionId.Add(item.SessionId);
                    }

                    //上报
                    callback(proxy.Key, lstSessionId);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        public bool Connect(string channelName, uint sessionId, string address, int port)
        {
            if (IsClosed) return false;
            var proxy = new ProxySocket(this, channelName, sessionId, address, port)
            {
                 FlowControl = this.FlowControl 
            };

            var isConnected = proxy.Connect();
            if (isConnected)
            {
                if (_proxy.ContainsKey(channelName))
                {
                    var list = _proxy[channelName];
                    lock (list)
                    {
                        list.Add(proxy);
                    }
                }
                else
                {
                    _proxy[channelName] = new List<ProxySocket> { proxy };
                }
            }
            if (OnProxyConnected != null) OnProxyConnected(proxy.ChannelName, proxy.SessionId, isConnected);
            return isConnected;

        }

        //如果 己经清掉的会话，就不再上报
        internal void RaiseEvent_OnDisconnect(string channelName, uint sessionId)
        {
            if (!IsClosed)
            {
                if (_proxy.TryGetValue(channelName, out var list))
                {
                    int count = 0;

                    lock (list)
                    {
                        count = list.RemoveAll(a => a.SessionId == sessionId);
                    }
                    if (count > 0 && OnProxyConnected != null) OnProxyConnected(channelName, sessionId, false);
                }
            }
        }



        public bool Disconnect(string channelName, uint sessionId)
        {
            bool result = false;
            ProxySocket? proxy = null;

            if (_proxy.TryGetValue(channelName, out var list))
            {
                lock (list)
                {
                    proxy = list.Find(p => p.SessionId == sessionId && p.ChannelName == channelName);
                    if (proxy != null) list.Remove(proxy);
                }

                if (proxy != null)
                {
                    proxy.Disconnect();
                    result = true;
                }
            }
            return result;
        }

        public void Dispose()
        {
            IsClosed = true;

            foreach (var item in _proxy)
            {
                var list = item.Value;
                foreach (var proxy in list)
                {
                    proxy.Disconnect();
                }
                list.Clear();
            }
            _proxy.Clear();
        }

        public bool ContainsSession(string channelName, uint sessionId)
        {
            bool result = false;
            if (_proxy.TryGetValue(channelName, out var list))
            {
                lock (list)
                {
                    result = list.Any(p => p.SessionId == sessionId && p.ChannelName == channelName);
                }
            }
            return result;
        }
        public async Task<bool> SendData(string channelName, uint sessionId, byte[] data)
        {
            bool result = false;
            if (IsClosed) return result;

            ProxySocket? proxy = null;

            if (_proxy.TryGetValue(channelName, out var list))
            {
                lock (list)
                {
                    proxy = list.Find(p => p.SessionId == sessionId && p.ChannelName == channelName);
                }
            }
            if (proxy != null)
            {
                result = await proxy.SendData(data);
                if (!result)
                {
                    proxy.Disconnect();
                }
            }
            return result;
        }
    }
}

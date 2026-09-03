using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace Tunneling.Core
{
    public class TcpServer
    {
        public Func<TcpServer, Socket, bool> CheckChannelFilter;
        public event Action<TcpServer> OnServerStarted;
        public event Action<TcpServer> OnServerStopped;

        public event EventHandler<TcpServer, TcpSession> OnSessionConnected;
        public event Action<TcpServer, TcpSession, byte[]> OnDataReceived;
        public event EventHandler<TcpServer, TcpSession> OnSessionDisconnected;



        private readonly List<TcpSession> _sessions = new List<TcpSession>();
        private Socket _sckServer;
        private SocketAsyncEventArgsPool _EventArgsPool;

        private System.Threading.Timer? _timer;
        public TcpServer(int port, SocketAsyncEventArgsPool pool)
        {
            Port = port;
            _EventArgsPool = pool;
        }

        public BanPolicy? Policy { get; set; } = null;
        public bool IsRunning { get; private set; } = false;
        public int Port { get; private set; } = 0;



        #region 统计数据 

        private ulong _totalReceiveBytes = 0;

        private ulong _totalSendBytes = 0;
        public ulong TotalReceiveBytes { get { return _totalReceiveBytes; } }
        public ulong TotalSendBytes { get { return _totalSendBytes; } }
        // 当前会话数
        public int SessionCount
        {
            get
            {
                lock (_sessions)
                {
                    return _sessions.Count;
                }
            }
        }


        // 统计请求数
        private long _totalRequestCount = 0;
        public long TotalRequestCount { get { return _totalRequestCount; } }
        #endregion
        #region 隧道相关信息
        public string GroupName { get; set; }
        public string ChannelName { get; set; }
        public string SessionName { get { return GroupName + "::" + ChannelName; } }
        public string PrivateHost { get; set; }
        public int PrivatePort { get; set; }
        #endregion

        public void Start()
        {
            if (IsRunning)
                throw new InvalidOperationException("Server is already running.");

            _sckServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _sckServer.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, Port));
            _sckServer.Listen(50);

            IsRunning = true;

            for (int i = 0; i < 20; i++)
            {
                var acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += On_IOCompleted;

                StartAccept(acceptEventArg);
            }

            OnServerStarted?.Invoke(this);

            // 心跳检测，避免死连接
            _timer = new Timer(On_TimerCallback, this, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20));
        }

        //定时器事件 心跳检查 
        private void On_TimerCallback(object? state)
        {
            try
            {
                var sessions = GetAllSessions();
                foreach (var session in sessions)
                {
                    session.CheckHealth();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public void Close()
        {
            if (!IsRunning)
                throw new InvalidOperationException("Server is not running.");

            IsRunning = false;

            _timer?.Dispose();
            _timer = null;

            _sckServer.Dispose();

            var tmp = new List<TcpSession>();
            lock (_sessions)
            {
                tmp = _sessions.ToList();
            }
            foreach (var session in tmp)
            {
                session.Close();
            }
            _sessions.Clear();

            OnServerStopped?.Invoke(this);
        }

        private void StartAccept(SocketAsyncEventArgs acceptArgs)
        {
            if (IsRunning)
            {
                acceptArgs.AcceptSocket = null;
                try
                {
                    if (!_sckServer.AcceptAsync(acceptArgs))
                    {
                        On_IOCompleted(null, acceptArgs);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }
            else
            {
                acceptArgs.Dispose();
            }
        }

        private void StartReceive(TcpSession session, SocketAsyncEventArgs e)
        {
            try
            {
                if (!session.Connection.ReceiveAsync(e))
                {
                    On_IOCompleted(session, e);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                session.Close();
            }
        }

        private void On_IOCompleted(object? sender, SocketAsyncEventArgs e)
        {

            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    On_Accept(e);
                    break;

                case SocketAsyncOperation.Receive:
                    On_Receive(e);
                    break;
                default:
                    throw new NotImplementedException("On_IOCompleted 未知操作");
            }
        }

        static volatile uint _sessionId = 0;
        static internal uint GetSessionId()
        {
            return Interlocked.Increment(ref _sessionId);
        }
        private void On_Accept(SocketAsyncEventArgs acceptEventArg)
        {
            if (IsRunning)
            {
                var clientSocket = acceptEventArg.AcceptSocket;

                // 过滤不符合条件的连接
                if (CheckChannelFilter(this, clientSocket))
                {
                    Interlocked.Increment(ref _totalRequestCount);

                    TcpSession session = new TcpSession(GetSessionId(), this, clientSocket, _EventArgsPool.Pop());
                    session.SocketAsyncEventArgs.Completed += On_IOCompleted;

                    lock (_sessions)
                    {
                        _sessions.Add(session);
                    }

                    OnSessionConnected?.Invoke(this, session);

                    StartReceive(session, session.SocketAsyncEventArgs);
                }
                else
                {
                    clientSocket.Dispose();
                }
                StartAccept(acceptEventArg);
            }
            else
            {
                acceptEventArg.Dispose();
            }
        }
        private void On_Receive(SocketAsyncEventArgs e)
        {
            var session = e.UserToken as TcpSession;

            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                Interlocked.Add(ref _totalReceiveBytes, (ulong)e.BytesTransferred);

                byte[] data = new byte[e.BytesTransferred];
                Buffer.BlockCopy(e.Buffer, e.Offset, data, 0, e.BytesTransferred);

                session?.ResetHealth();
                OnDataReceived?.Invoke(this, session, data);
                StartReceive(session, e);
            }
            else
            {
                session?.Close();
            }
        }

        internal void CloseSession(TcpSession session)
        {
            bool isRemoved = false;

            if (session == null) return;

            lock (_sessions)
            {
                isRemoved = _sessions.Remove(session);
            }

            if (isRemoved)
            {
                session.SocketAsyncEventArgs.Completed -= On_IOCompleted;
                _EventArgsPool.Push(session.SocketAsyncEventArgs);

                OnSessionDisconnected?.Invoke(this, session);
            }
        }

        public async Task<bool> SendData(uint sessionId, byte[] data)
        {
            bool ok = false;

            if (IsRunning)
            {
                var session = GetSessionById(sessionId);
                if (session != null && !session.IsClosed && data.Length > 0)
                {
                    try
                    {
                        ok = await session.Send(data);
                        if (ok)
                        {
                            Interlocked.Add(ref _totalSendBytes, (ulong)data.Length);
                        }
                        else
                        {
                            session.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        session.Close();
                    }
                }
            }
            return ok;
        }

        public TcpSession? GetSessionById(uint id)
        {
            lock (_sessions)
            {
                return _sessions.FirstOrDefault(s => s.Id == id);
            }
        }

        public List<TcpSession> GetAllSessions()
        {
            lock (_sessions)
            {
                return _sessions.ToList();
            }
        }
    }
}

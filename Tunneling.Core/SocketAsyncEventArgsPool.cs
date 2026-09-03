using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Tunneling.Core
{
    public class SocketAsyncEventArgsPool : IDisposable
    {
        private readonly Stack<SocketAsyncEventArgs> _pool;

        private readonly byte[] _buffer;
        private readonly Semaphore _semaphore;

        public SocketAsyncEventArgsPool(int capacity, int bufferSize = 8192)
        {
            _pool = new Stack<SocketAsyncEventArgs>(capacity);

            _buffer = new byte[capacity * bufferSize];

            for (int i = 0; i < capacity; i++)
            {
                var args = new SocketAsyncEventArgs();
                args.SetBuffer(_buffer, i * bufferSize, bufferSize);

                _pool.Push(args);
            }

            _semaphore = new Semaphore(capacity, capacity);
        }
        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            item.UserToken = null;

            lock (_pool)
            {
                _pool.Push(item);
            }
            _semaphore.Release();
        }
        public SocketAsyncEventArgs Pop()
        {
            if (!_semaphore.WaitOne(TimeSpan.FromSeconds(5)))
            {
                Console.WriteLine("SocketAsyncEventArgs  Semaphore.WaitOne  死锁");
            }
            lock (_pool)
            {
                return _pool.Pop();
            }
        }

        public int Count
        {
            get
            {
                lock (_pool)
                {
                    return _pool.Count;
                }
            }
        }

        #region IDisposable 接口实现

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    while (_pool.Count > 0)
                    {
                        //var args = _pool.Dequeue();
                        var args = _pool.Pop();
                        args.Dispose();
                    }

                    _pool.Clear();
                    _semaphore.Dispose();
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~SocketAsyncEventArgsPool()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}

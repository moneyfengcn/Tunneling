namespace Tunneling.Server.Framework
{
    public delegate Task<bool> UploadStreamDelegate(string channelName, uint sessionId, byte[] data);

    public delegate void SessionEventDelegate<T>(string channelName, T sessionId);

    /// <summary>
    /// 隧道上行事件广播接口
    /// </summary>
    public interface ITunnelUploadChannel
    {
        Task<bool> UploadStream(string channelName, uint sessionId, byte[] data);
        void SessionConnected(string channelName, uint sessionId);
        void CloseSession(string channelName, uint sessionId);
        void SessionChecked(string channelName, uint sessionId);
        void SyncConversationList(string channelName, List<uint> listSessionId);
        /// <summary>
        /// 隧道关闭通知
        /// </summary>
        /// <param name="group"></param>
        void ChannelClosed(string group);

        event Action<string> OnChannelClosedEvent;

        event UploadStreamDelegate OnUploadStreamEvent;
        event SessionEventDelegate<uint> OnSessionConnectedEvent;
        event SessionEventDelegate<uint> OnCloseSessionEvent;
        event SessionEventDelegate<uint> OnSessionCheckedEvent;
        event SessionEventDelegate<List<uint>> OnSyncConversationListEvent;
    }
    public class TunnelUploadChannelServices : ITunnelUploadChannel
    {
        public event UploadStreamDelegate OnUploadStreamEvent;
        public event SessionEventDelegate<uint> OnSessionConnectedEvent;
        public event SessionEventDelegate<uint> OnCloseSessionEvent;
        public event SessionEventDelegate<uint> OnSessionCheckedEvent;
        public event SessionEventDelegate<List<uint>> OnSyncConversationListEvent;
        public event Action<string> OnChannelClosedEvent;

        public void ChannelClosed(string group)
        {
            OnChannelClosedEvent?.Invoke(group);
        }

        public void CloseSession(string channelName, uint sessionId)
        {
            OnCloseSessionEvent?.Invoke(channelName, sessionId);
        }

        public void SessionChecked(string channelName, uint sessionId)
        {
            OnSessionCheckedEvent?.Invoke(channelName, sessionId);
        }

        public void SessionConnected(string channelName, uint sessionId)
        {
            OnSessionConnectedEvent?.Invoke(channelName, sessionId);
        }

        public void SyncConversationList(string channelName, List<uint> listSessionId)
        {
            OnSyncConversationListEvent?.Invoke(channelName, listSessionId);
        }

        public async Task<bool> UploadStream(string channelName, uint sessionId, byte[] data)
        {
            return await OnUploadStreamEvent?.Invoke(channelName, sessionId, data);
        }
    }
}

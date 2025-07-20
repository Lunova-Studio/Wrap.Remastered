using STUN.Enums;
using System.Net;
using Wrap.Remastered.Client;
using Wrap.Remastered.Network.Protocol;
using Wrap.Remastered.Network.Protocol.ClientBound;
using Wrap.Remastered.Network.Protocol.PeerBound;
using Wrap.Remastered.Network.Protocol.ServerBound;
using Wrap.Remastered.Schemas;

namespace Wrap.Remastered.Interfaces
{
    public interface IWrapClient
    {
        ClientCommandManager ClientCommandManager { get; }
        NatType CurrentNatType { get; }
        RoomInfoPacket? CurrentRoomInfo { get; }
        string? DisplayName { get; }
        bool Disposed { get; }
        bool IsConnected { get; }
        bool IsLoggedIn { get; }
        RoomDismissedPacket? LastRoomDismissed { get; }
        RoomInfoQueryResultPacket? LastRoomInfoQueryResult { get; }
        RoomJoinRequestNoticePacket? LastRoomJoinRequestNotice { get; }
        RoomJoinResultPacket? LastRoomJoinResult { get; }
        RoomOwnerChangedPacket? LastRoomOwnerChanged { get; }
        UserInfoResultPacket? LastUserInfoResult { get; }
        LocalProxyServer? LocalProxyServer { get; }
        string? Name { get; }
        PeerConnectionManager PeerConnectionManager { get; }
        IReadOnlyList<string> PendingJoinUserIds { get; }
        UserProfile Profile { get; }
        ProxyManager? ProxyManager { get; }
        IPEndPoint? PublicEndPoint { get; }
        IPEndPoint? RemoteIP { get; set; }
        IUPnPService? UPnPService { get; set; }
        string? UserId { get; }

        event EventHandler? Connected;
        event EventHandler<UnsolvedPacket>? DataReceived;
        event EventHandler<string>? Disconnected;
        event EventHandler<KeepAlivePacket>? KeepAliveReceived;
        event EventHandler<UserInfo>? LoggedIn;
        event EventHandler<NatType>? NatTypeDetected;
        event EventHandler<IClientBoundPacket>? PacketReceived;
        event EventHandler<PeerConnectAcceptNoticePacket>? PeerConnectAcceptReceived;
        event EventHandler<PeerConnectFailedNoticePacket>? PeerConnectFailedReceived;
        event EventHandler<PeerConnectRejectNoticePacket>? PeerConnectRejectReceived;
        event EventHandler<PeerConnectRequestNoticePacket>? PeerConnectRequestReceived;
        event EventHandler<PeerConnectSuccessPacket>? PeerConnectSuccessReceived;
        event EventHandler<PeerIPInfoPacket>? PeerIPInfoReceived;
        event EventHandler<RoomChatMessagePacket>? RoomChatMessageReceived;
        event EventHandler<RoomDismissedPacket>? RoomDismissed;
        event EventHandler<RoomInfoQueryResultPacket>? RoomInfoQueryResultReceived;
        event EventHandler<RoomInfoPacket>? RoomInfoReceived;
        event EventHandler<RoomJoinRequestNoticePacket>? RoomJoinRequestNoticeReceived;
        event EventHandler<RoomJoinResultPacket>? RoomJoinResultReceived;
        event EventHandler<RoomJoinResultPacket>? RoomKickResultReceived;
        event EventHandler<RoomOwnerChangedPacket>? RoomOwnerChanged;
        event EventHandler<UserInfoResultPacket>? UserInfoResultReceived;

        void ApproveJoinRoom(int roomId, string userId);
        void CloseAllPeerConnections();
        void ClosePeerConnection(string targetUserId);
        void Connect(string serverAddress, int port = 10270);
        Task ConnectAsync(string serverAddress, int port = 10270);
        void Disconnect();
        Task DisconnectAsync();
        void DismissRoom(int roomId);
        void Dispose();
        ClientCommandManager GetClientCommandManager();
        IEnumerable<string> GetPeerConnections();
        ConnectionStatus GetPeerConnectionStatus(string targetUserId);
        Dictionary<string, object> GetPeerKeepAliveStatus();
        ProxyStatistics GetProxyStatistics();
        UserInfo? GetUserInfo();
        bool HasPeerConnection(string targetUserId);
        void KickUserFromRoom(int roomId, string userId);
        void QueryUserInfo(string userId);
        void RejectJoinRoom(int roomId, string userId);
        void RequestJoinRoom(int roomId);
        Task SendLoginPacketAsync();
        Task SendLoginPacketAsync(UserInfo userInfo);
        void SendPacket(IServerBoundPacket packet);
        Task SendPacketAsync(IServerBoundPacket packet);
        void SendPeerKeepAlive(string targetUserId);
        Task SendPeerKeepAliveAsync(string targetUserId);
        Task SendPeerPacketAsync(string targetUserId, IPeerBoundPacket packet);
        void SetPeerHeartbeatInterval(int intervalSeconds);
        void SetUserInfo(UserInfo userInfo);
        void TransferRoomOwner(int roomId, string newOwnerUserId);
    }
}
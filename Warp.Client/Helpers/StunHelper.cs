using Microsoft;
using STUN.Client;
using STUN.Enums;
using STUN.Proxy;
using STUN.StunResult;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Warp.Client.Helpers;

/// <summary>
/// STUN工具类
/// </summary>
public static class StunHelper {
    public static string STUNServer { get; set; } = "stun.hot-chilli.net";

    public static async Task<IPEndPoint> GetRemoteIPAsync(IPEndPoint localIp) {
        using StunClient3489 stunClient = new(new((await Dns.GetHostAddressesAsync(STUNServer)).First(), 3478), localIp);
        await stunClient.QueryAsync();
        return stunClient.State.PublicEndPoint!;
    }
    public static async Task<IPEndPoint> GetRemoteIPAsync(Socket socket) {
        using StunClient3489 stunClient = new(new((await Dns.GetHostAddressesAsync(STUNServer)).First(), 3478), (IPEndPoint)socket.LocalEndPoint!, new NoneUdpProxy(socket));
        await stunClient.QueryAsync();
        return stunClient.State.PublicEndPoint!;
    }
    public static async Task<NatType> GetNatTypeAsync() {
        using StunClient3489 stunClient = new(new((await Dns.GetHostAddressesAsync(STUNServer)).First(), 3478), IPEndPoint.Parse("0.0.0.0"));
        await stunClient.QueryAsync();
        return stunClient.State.NatType;
    }

    public static async Task<ClassicStunResult> GetClassicStunResultAsync(IPEndPoint localIp) {
        using StunClient3489 stunClient = new(new((await Dns.GetHostAddressesAsync(STUNServer)).First(), 3478), IPEndPoint.Parse("0.0.0.0"), new NoneUdpProxy(localIp));
        await stunClient.QueryAsync();
        return stunClient.State;
    }

    public static async Task<ClassicStunResult> GetClassicStunResultAsync() {
        using StunClient3489 stunClient = new(new((await Dns.GetHostAddressesAsync(STUNServer)).First(), 3478), IPEndPoint.Parse("0.0.0.0"));
        await stunClient.QueryAsync();
        return stunClient.State;
    }
}

public sealed class NoneUdpProxy : IUdpProxy {
    public Socket Client { get; }

    public NoneUdpProxy(IPEndPoint localEndPoint) {
        Requires.NotNull(localEndPoint, nameof(localEndPoint));

        Client = new Socket(localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        Client.Bind(localEndPoint);
    }

    public NoneUdpProxy(Socket client) {
        Requires.NotNull(client, nameof(client));

        Client = client;
    }
    public ValueTask ConnectAsync(CancellationToken cancellationToken = default) {
        return default;
    }

    public ValueTask CloseAsync(CancellationToken cancellationToken = default) {
        return default;
    }

    public ValueTask<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken = default) {
        return Client.ReceiveMessageFromAsync(buffer, socketFlags, remoteEndPoint, cancellationToken);
    }

    public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP, CancellationToken cancellationToken = default) {
        return Client.SendToAsync(buffer, socketFlags, remoteEP, cancellationToken);
    }

    public void Dispose() {
        Client.Dispose();
        GC.SuppressFinalize(this);
    }
}
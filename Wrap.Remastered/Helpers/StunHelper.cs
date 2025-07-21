using System.Net;
using System.Net.Sockets;
using STUN.Enums;
using STUN.StunResult;
using STUN.Client;
using Wrap.Remastered.STUN;

namespace Wrap.Remastered.Helpers;

/// <summary>
/// STUN工具类
/// </summary>
public static class StunHelper
{
    public static string STUNServer { get; set; } = "stun.hot-chilli.net";
    public static async Task<IPEndPoint> GetRemoteIPAsync(IPEndPoint localIp)
    {
        using StunClient3489 stunClient = new(new((await Dns.GetHostAddressesAsync(STUNServer)).First(), 3478), localIp);
        await stunClient.QueryAsync();
        return stunClient.State.PublicEndPoint!;
    }
    public static async Task<IPEndPoint> GetRemoteIPAsync(Socket socket)
    {
        using StunClient3489 stunClient = new(new((await Dns.GetHostAddressesAsync(STUNServer)).First(), 3478), (IPEndPoint)socket.LocalEndPoint!, new NoneUdpProxy((IPEndPoint)socket.LocalEndPoint!));
        await stunClient.QueryAsync();
        return stunClient.State.PublicEndPoint!;
    }
    public static async Task<NatType> GetNatTypeAsync()
    {
        using StunClient3489 stunClient = new(new((await Dns.GetHostAddressesAsync(STUNServer)).First(), 3478), IPEndPoint.Parse("0.0.0.0"));
        await stunClient.QueryAsync();
        return stunClient.State.NatType;
    }

    public static async Task<ClassicStunResult> GetClassicStunResultAsync(IPEndPoint localIp)
    {
        using StunClient3489 stunClient = new(new((await Dns.GetHostAddressesAsync(STUNServer)).First(), 3478), IPEndPoint.Parse("0.0.0.0"), new NoneUdpProxy(localIp));
        await stunClient.QueryAsync();
        return stunClient.State;
    }

    public static async Task<ClassicStunResult> GetClassicStunResultAsync()
    {
        using StunClient3489 stunClient = new(new((await Dns.GetHostAddressesAsync(STUNServer)).First(), 3478), IPEndPoint.Parse("0.0.0.0"));
        await stunClient.QueryAsync();
        return stunClient.State;
    }
}


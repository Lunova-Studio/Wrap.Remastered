using Wrap.Remastered.Interfaces;
using System.Net;

namespace Wrap.Remastered;

/// <summary>
/// UPnP服务实现
/// </summary>
public class UPnPService : IUPnPService
{
    public Waher.Networking.UPnP.UPnPService Service { get; private set; }
    public UPnPService(Waher.Networking.UPnP.UPnPService service) { Service = service; }
    public void AddPortMapping(IPAddress? NewRemoteHost, int NewExternalPort, IUPnPService.SocketProtocol NewProtocol, IPAddress NewInternalClient, int NewInternalPort, bool NewEnabled, string NewPortMappingDescription, TimeSpan NewLeaseDuration)
    {
        Service.GetService().InvokeAsync("AddPortMapping", 3000,
                new KeyValuePair<string, object>("NewRemoteHost", (NewRemoteHost is null) ? GetExternalIPAddress().ToString() : NewRemoteHost.ToString()),
                new KeyValuePair<string, object>("NewExternalPort", NewExternalPort),
                new KeyValuePair<string, object>("NewProtocol", NewProtocol.ToString()),
                new KeyValuePair<string, object>("NewInternalPort", NewInternalPort),
                new KeyValuePair<string, object>("NewInternalClient", NewInternalClient.ToString()),
                new KeyValuePair<string, object>("NewEnabled", true),
                new KeyValuePair<string, object>("NewPortMappingDescription", NewPortMappingDescription),
                new KeyValuePair<string, object>("NewLeaseDuration", NewLeaseDuration.TotalSeconds)).GetAwaiter().GetResult();
    }

    public async Task AddPortMappingAsync(IPAddress? NewRemoteHost, int NewExternalPort, IUPnPService.SocketProtocol NewProtocol, IPAddress NewInternalClient, int NewInternalPort, bool NewEnabled, string NewPortMappingDescription, TimeSpan NewLeaseDuration)
    {
        await (await Service.GetServiceAsync()).InvokeAsync("AddPortMapping", 3000,
                new KeyValuePair<string, object>("NewRemoteHost", (NewRemoteHost is null) ? (await GetExternalIPAddressAsync()).ToString() : NewRemoteHost.ToString()),
                new KeyValuePair<string, object>("NewExternalPort", NewExternalPort),
                new KeyValuePair<string, object>("NewProtocol", NewProtocol.ToString()),
                new KeyValuePair<string, object>("NewInternalPort", NewInternalPort),
                new KeyValuePair<string, object>("NewInternalClient", NewInternalClient.ToString()),
                new KeyValuePair<string, object>("NewEnabled", true),
                new KeyValuePair<string, object>("NewPortMappingDescription", NewPortMappingDescription),
                new KeyValuePair<string, object>("NewLeaseDuration", NewLeaseDuration.TotalSeconds));
    }

    public void DeletePortMapping(IPAddress? NewRemoteHost, int NewExternalPort, IUPnPService.SocketProtocol NewProtocol)
    {
        Service.GetService().InvokeAsync("DeletePortMapping", 3000,
                new KeyValuePair<string, object>("NewRemoteHost", (NewRemoteHost is null) ? GetExternalIPAddress().ToString() : NewRemoteHost.ToString()),
                new KeyValuePair<string, object>("NewExternalPort", NewExternalPort),
                new KeyValuePair<string, object>("NewProtocol", NewProtocol.ToString())).GetAwaiter().GetResult();
    }

    public async Task DeletePortMappingAsync(IPAddress? NewRemoteHost, int NewExternalPort, IUPnPService.SocketProtocol NewProtocol)
    {
        await (await Service.GetServiceAsync()).InvokeAsync("DeletePortMapping", 3000,
                new KeyValuePair<string, object>("NewRemoteHost", (NewRemoteHost is null) ? (await GetExternalIPAddressAsync()).ToString() : NewRemoteHost.ToString()),
                new KeyValuePair<string, object>("NewExternalPort", NewExternalPort),
                new KeyValuePair<string, object>("NewProtocol", NewProtocol.ToString()));
    }

    public IPAddress GetExternalIPAddress()
    {
        string address = (string)Service.GetService().InvokeAsync("GetExternalIPAddress", 3000).GetAwaiter().GetResult().Value["NewExternalIPAddress"];
        return IPAddress.Parse(address);
    }

    public async Task<IPAddress> GetExternalIPAddressAsync()
    {
        string address = (string)(await((await Service.GetServiceAsync()).InvokeAsync("GetExternalIPAddress", 3000))).Value["NewExternalIPAddress"];
        return IPAddress.Parse(address);
    }

    public Waher.Networking.UPnP.UPnPService GetUPnPService()
    {
        return Service;
    }
} 
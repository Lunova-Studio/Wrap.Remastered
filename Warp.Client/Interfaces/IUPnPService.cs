using System.Net;
using Warp.Client.Helpers;
using Wrap.Shared.Enums;

namespace Warp.Client.Interfaces;

public interface IUPnPService {
    public static IPAddress? InternalIPAddressCache { get; private set; } = null;

    IPAddress GetExternalIPAddress();

    void DeletePortMapping(IPAddress? NewRemoteHost, int NewExternalPort, SocketProtocol NewProtocol);

    void AddPortMapping(IPAddress? NewRemoteHost, int NewExternalPort, SocketProtocol NewProtocol, IPAddress NewInternalClient, int NewInternalPort, bool NewEnabled, string NewPortMappingDescription, TimeSpan NewLeaseDuration);

    Task DeletePortMappingAsync(IPAddress? NewRemoteHost, int NewExternalPort, SocketProtocol NewProtocol);

    Task AddPortMappingAsync(IPAddress? NewRemoteHost, int NewExternalPort, SocketProtocol NewProtocol, IPAddress NewInternalClient, int NewInternalPort, bool NewEnabled, string NewPortMappingDescription, TimeSpan NewLeaseDuration);

    void AddPortMapping(int NewExternalPort, SocketProtocol NewProtocol, int NewInternalPort, bool NewEnabled, string NewPortMappingDescription, TimeSpan NewLeaseDuration) {
        AddPortMapping(null, NewExternalPort, NewProtocol, GetInternalIpAddress(), NewInternalPort, NewEnabled, NewPortMappingDescription, NewLeaseDuration);
    }

    async Task AddPortMappingAsync(int NewExternalPort, SocketProtocol NewProtocol, int NewInternalPort, bool NewEnabled, string NewPortMappingDescription, TimeSpan NewLeaseDuration) {
        await AddPortMappingAsync(null, NewExternalPort, NewProtocol, GetInternalIpAddress(), NewInternalPort, NewEnabled, NewPortMappingDescription, NewLeaseDuration);
    }

    void AddPortMapping(int NewExternalPort, SocketProtocol NewProtocol, int NewInternalPort, bool NewEnabled, string NewPortMappingDescription) {
        AddPortMapping(NewExternalPort, NewProtocol, NewInternalPort, NewEnabled, NewPortMappingDescription, TimeSpan.Zero);
    }

    async Task AddPortMappingAsync(int NewExternalPort, SocketProtocol NewProtocol, int NewInternalPort, bool NewEnabled, string NewPortMappingDescription) {
        await AddPortMappingAsync(NewExternalPort, NewProtocol, NewInternalPort, NewEnabled, NewPortMappingDescription, TimeSpan.Zero);
    }

    void AddPortMapping(int NewExternalPort, SocketProtocol NewProtocol, int NewInternalPort, string NewPortMappingDescription, TimeSpan NewLeaseDuration) {
        AddPortMapping(NewExternalPort, NewProtocol, NewInternalPort, true, NewPortMappingDescription, NewLeaseDuration);
    }

    async Task AddPortMappingAsync(int NewExternalPort, SocketProtocol NewProtocol, int NewInternalPort, string NewPortMappingDescription, TimeSpan NewLeaseDuration) {
        await AddPortMappingAsync(NewExternalPort, NewProtocol, NewInternalPort, true, NewPortMappingDescription, NewLeaseDuration);
    }

    void AddPortMapping(int NewExternalPort, SocketProtocol NewProtocol, int NewInternalPort, string NewPortMappingDescription) {
        AddPortMapping(NewExternalPort, NewProtocol, NewInternalPort, true, NewPortMappingDescription);
    }

    async Task AddPortMappingAsync(int NewExternalPort, SocketProtocol NewProtocol, int NewInternalPort, string NewPortMappingDescription) {
        await AddPortMappingAsync(NewExternalPort, NewProtocol, NewInternalPort, true, NewPortMappingDescription);
    }

    void AddPortMapping(int NewExternalPort, SocketProtocol NewProtocol, int NewInternalPort) {
        AddPortMapping(NewExternalPort, NewProtocol, NewInternalPort, "");
    }

    async Task AddPortMappingAsync(int NewExternalPort, SocketProtocol NewProtocol, int NewInternalPort) {
        await AddPortMappingAsync(NewExternalPort, NewProtocol, NewInternalPort, "");
    }

    void DeletePortMapping(int NewExternalPort, SocketProtocol NewProtocol) {
        DeletePortMapping(null, NewExternalPort, NewProtocol);
    }

    async Task DeletePortMappingAsync(int NewExternalPort, SocketProtocol NewProtocol) {
        await DeletePortMappingAsync(null, NewExternalPort, NewProtocol);
    }

    public static IPAddress GetInternalIpAddress() {
        return NetworkHelper.GetInternalIpAddress()!;
    }
}

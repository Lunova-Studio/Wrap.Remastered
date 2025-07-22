using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Warp.Client.Helpers;

public static class NetworkHelper {
    public static IPAddress? GetInternalIpAddress() {
        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic =>
                nic.OperationalStatus == OperationalStatus.Up &&
                nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                !IsVirtualNic(nic) &&
                HasInternetGateway(nic));

        foreach (var nic in networkInterfaces) {
            var ipv4Address = nic.GetIPProperties().UnicastAddresses
                .FirstOrDefault(addr =>
                    addr.Address.AddressFamily == AddressFamily.InterNetwork);

            if (ipv4Address != null)
                return ipv4Address.Address;
        }

        return null;
    }

    public static bool IsVirtualNic(NetworkInterface nic) {
        string[] virtualNicKeywords = [
            "virtual", "vmware", "vbox", "hyper-v", "loopback", "ppp", "tunnel"
        ];

        string nicName = nic.Name.ToLower();
        return virtualNicKeywords.Any(keyword => nicName.Contains(keyword));
    }

    public static bool HasInternetGateway(NetworkInterface nic) {
        return nic.GetIPProperties().GatewayAddresses
            .Any(gateway => gateway.Address.AddressFamily == AddressFamily.InterNetwork);
    }
}
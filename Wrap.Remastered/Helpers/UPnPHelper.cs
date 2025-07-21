using ConsoleInteractive;
using System.Threading;
using Waher.Networking.UPnP;
using Wrap.Remastered.Interfaces;

namespace Wrap.Remastered.Helpers;

public static class UPnPHelper
{
    public static async Task<IUPnPService?> LookUpUPnPDeviceAsync(TimeSpan timeout)
    {
        try
        {
            CancellationTokenSource cts = new CancellationTokenSource(timeout);
            List<UPnPDevice> UPnPDeviceLocations = new();

            bool Searching = true;
            UPnPClient client = new();
            IUPnPService? uPnP = null;
            client.OnDeviceFound += Client_OnDeviceFound;

            async Task Client_OnDeviceFound(object Sender, DeviceLocationEventArgs e)
            {
                UPnPDevice device = (await e.Location.GetDeviceAsync()).Device;
                if (device.DeviceType != "urn:schemas-upnp-org:device:InternetGatewayDevice:1") return;
                if (e.RemoteEndPoint.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return;

                if (Searching)
                    ConsoleWriter.WriteLine(device.FriendlyName);
                UPnPDeviceLocations.Add(device);
            }

            await client.StartSearch();

            DateTime time = DateTime.Now;
            while ((DateTime.Now - time) < timeout && UPnPDeviceLocations.Count == 0)
            {
                await Task.Delay(100, cts.Token);
            }

            Searching = false;

            foreach (UPnPDevice UPnPDeviceLocation in UPnPDeviceLocations)
            {
                if (UPnPDeviceLocation != null)
                {
                    Waher.Networking.UPnP.UPnPService? natService = UPnPDeviceLocation.GetService("urn:schemas-upnp-org:service:WANIPConnection:1"); // 获取WAN IP连接服务
                    natService ??= UPnPDeviceLocation.GetService("urn:schemas-upnp-org:service:WANPPPConnection:1");

                    if (natService != null)
                    {
                        uPnP = new UPnPService(natService);
                        _ = cts.CancelAsync();
                        return uPnP;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            return null;
        }

        return null;
    }
}

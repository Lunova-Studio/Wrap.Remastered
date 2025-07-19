using Wrap.Remastered.Client;
using Wrap.Remastered.Schemas;
using Wrap.Remastered.Network.Protocol.ServerBound;
using ConsoleInteractive;

namespace Wrap.Remastered.Console;

class Program
{
    static async Task Main(string[] args)
    {
        ConsoleWriter.WriteLine("=== Wrap.Remastered 客户端测试 ===");

        var userInfo = new UserInfo
        {
            UserId = "test_user_001",
            Name = "测试用户",
            DisplayName = "测试用户"
        };

        using var client = new WrapClient(userInfo);

        // 注册事件处理器
        client.Connected += OnConnected;
        client.Disconnected += OnDisconnected;
        client.DataReceived += OnDataReceived;

        try
        {
            ConsoleWriter.WriteLine("5秒后连接到服务器...");
            await Task.Delay(5000);

            await client.ConnectAsync("localhost", 10270);

            ConsoleWriter.WriteLine("连接成功！发送测试数据包...");

            var testPacket = new LoginPacket(userInfo);
            await client.SendPacketAsync(testPacket);

            ConsoleWriter.WriteLine("测试数据包已发送！按任意键断开连接...");
            System.Console.ReadKey();

            await client.DisconnectAsync();
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"连接过程中发生错误: {ex.Message}");
        }

        ConsoleWriter.WriteLine("程序结束。按任意键退出...");
        System.Console.ReadKey();
    }

    private static void OnConnected(object? sender, EventArgs e)
    {
        ConsoleWriter.WriteLine("客户端已连接");
    }

    private static void OnDisconnected(object? sender, EventArgs e)
    {
        ConsoleWriter.WriteLine("客户端已断开");
    }

    private static void OnDataReceived(object? sender, byte[] data)
    {
        ConsoleWriter.WriteLine($"接收到数据: {data.Length} 字节");
        
        // 这里可以添加数据包解析逻辑
        try
        {
            // 尝试解析为登录响应数据包
            // var response = LoginPacket.Serializer.Deserialize(data);
            // System.Console.WriteLine($"解析为登录响应数据包");
        }
        catch (Exception ex)
        {
            ConsoleWriter.WriteLine($"数据包解析失败: {ex.Message}");
        }
    }
}

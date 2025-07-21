# Wrap.Remastered 快速参考指南

## 🚀 快速开始

### 构建项目
```bash
# 构建整个解决方案
dotnet build

# 构建特定项目
dotnet build Wrap.Remastered.Server
dotnet build Wrap.Remastered.Console
```

### 运行应用程序
```bash
# 运行服务器
cd Wrap.Remastered.Server
dotnet run

# 运行客户端
cd Wrap.Remastered.Console
dotnet run
```

### Docker 部署
```bash
# 构建镜像
docker build -t wrap-remastered-server -f Wrap.Remastered.Server/Dockerfile .

# 运行容器
docker run -p 10270:10270 wrap-remastered-server
```

## 📦 数据包开发

### 创建新的服务器端数据包

1. **添加枚举类型**
```csharp
// ServerBoundPacketType.cs
public enum ServerBoundPacketType
{
    LoginPacket,
    ChatMessagePacket  // 新增
}
```

2. **创建数据包类**
```csharp
// ChatMessagePacket.cs
public class ChatMessagePacket : IServerBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new ChatMessagePacketSerializer();
    
    public string UserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;

    public ChatMessagePacket(string userId, string message, string roomId)
    {
        UserId = userId;
        Message = message;
        RoomId = roomId;
    }

    public ChatMessagePacket() { }

    public ISerializer<IPacket> GetSerializer()
    {
        return Serializer;
    }

    public void OnSerialize(ref byte[] data)
    {
        // 可选的额外序列化逻辑
    }

    public ServerBoundPacketType GetPacketType()
    {
        return ServerBoundPacketType.ChatMessagePacket;
    }
}
```

3. **创建序列化器**
```csharp
public class ChatMessagePacketSerializer : ISerializer<IPacket>
{
    public IPacket Deserialize(byte[] data)
    {
        ChatMessagePacket packet = new();

        using MemoryStream stream = new(data);

        packet.UserId = stream.ReadString();
        packet.Message = stream.ReadString();
        packet.RoomId = stream.ReadString();

        return packet;
    }

    public byte[] Serialize(IPacket obj)
    {
        if (obj is not ChatMessagePacket chatPacket) throw new ArgumentException();

        using MemoryStream stream = new();

        stream.WriteString(chatPacket.UserId);
        stream.WriteString(chatPacket.Message);
        stream.WriteString(chatPacket.RoomId);

        return stream.ToArray();
    }
}
```

4. **注册序列化器**
```csharp
// IServerBoundPacket.cs
public static Dictionary<ServerBoundPacketType, ISerializer<IPacket>> Serializers = new()
{
    { ServerBoundPacketType.LoginPacket, new LoginPacketSerializer() },
    { ServerBoundPacketType.ChatMessagePacket, new ChatMessagePacketSerializer() }  // 新增
};
```

5. **创建处理器**
```csharp
// ChatMessagePacketHandler.cs
public class ChatMessagePacketHandler : BasePacketHandler
{
    public ChatMessagePacketHandler(IConnectionManager connectionManager) : base(connectionManager)
    {
    }

    protected override void OnHandle(IChannel channel, UnsolvedPacket packet)
    {
        var chatPacket = ChatMessagePacket.Serializer.Deserialize(packet.Data) as ChatMessagePacket;
        if (chatPacket == null)
        {
            LogInfo(channel, packet, "聊天消息数据包反序列化失败");
            return;
        }

        LogInfo(channel, packet, $"用户 {chatPacket.UserId} 在房间 {chatPacket.RoomId} 发送消息: {chatPacket.Message}");
        
        // 处理聊天消息逻辑
        // ...
    }
}
```

6. **注册处理器**
```csharp
// PacketHandlerFactory.cs
_handlers = new Dictionary<int, IPacketHandler>
{
    { (int)ServerBoundPacketType.LoginPacket, new LoginPacketHandler(connectionManager) },
    { (int)ServerBoundPacketType.ChatMessagePacket, new ChatMessagePacketHandler(connectionManager) }  // 新增
};
```

### 创建新的客户端数据包

1. **添加枚举类型**
```csharp
// ClientBoundPacketType.cs
public enum ClientBoundPacketType
{
    LoginSucceedPacket,
    LoginFailedPacket,
    ChatMessageResponsePacket  // 新增
}
```

2. **创建数据包类**
```csharp
// ChatMessageResponsePacket.cs
public class ChatMessageResponsePacket : IClientBoundPacket
{
    public static ISerializer<IPacket> Serializer { get; } = new ChatMessageResponsePacketSerializer();
    
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public ChatMessageResponsePacket(string senderId, string senderName, string message, string roomId)
    {
        SenderId = senderId;
        SenderName = senderName;
        Message = message;
        RoomId = roomId;
        Timestamp = DateTime.Now;
    }

    public ChatMessageResponsePacket() { }

    public ISerializer<IPacket> GetSerializer()
    {
        return Serializer;
    }

    public void OnSerialize(ref byte[] data)
    {
        // 可选的额外序列化逻辑
    }

    public ClientBoundPacketType GetPacketType()
    {
        return ClientBoundPacketType.ChatMessageResponsePacket;
    }
}
```

3. **注册序列化器**
```csharp
// IClientBoundPacket.cs
public static Dictionary<ClientBoundPacketType, ISerializer<IPacket>> Serializers = new()
{
    { ClientBoundPacketType.LoginSucceedPacket, new LoginSucceedPacketSerializer() },
    { ClientBoundPacketType.LoginFailedPacket, new LoginFailedPacketSerializer() },
    { ClientBoundPacketType.ChatMessageResponsePacket, new ChatMessageResponsePacketSerializer() }  // 新增
};
```

## 🔧 常用代码片段

### 发送数据包
```csharp
// 客户端发送数据包
var packet = new LoginPacket(userInfo);
await client.SendPacketAsync(packet);

// 服务器发送数据包
var responsePacket = new LoginSucceedPacket(userInfo);
var serializer = responsePacket.GetSerializer();
var data = serializer.Serialize(responsePacket);
responsePacket.OnSerialize(ref data);

var packetData = new byte[4 + data.Length];
BitConverter.GetBytes((int)responsePacket.GetPacketType()).CopyTo(packetData, 0);
data.CopyTo(packetData, 4);

var buffer = Unpooled.WrappedBuffer(packetData);
await channel.WriteAndFlushAsync(buffer);
```

### 处理客户端事件
```csharp
client.Connected += OnConnected;
client.Disconnected += OnDisconnected;
client.DataReceived += OnDataReceived;
client.PacketReceived += OnPacketReceived;

private static void OnPacketReceived(object? sender, IClientBoundPacket packet)
{
    switch (packet)
    {
        case LoginSucceedPacket loginSucceed:
            Console.WriteLine($"登录成功: {loginSucceed.Name}");
            break;
        case LoginFailedPacket loginFailed:
            Console.WriteLine($"登录失败: {loginFailed.ErrorMessage}");
            break;
        case ChatMessageResponsePacket chatMessage:
            Console.WriteLine($"{chatMessage.SenderName}: {chatMessage.Message}");
            break;
    }
}
```

### 连接管理
```csharp
// 获取连接统计
var stats = connectionManager.GetStatistics();
Console.WriteLine($"活跃连接: {stats.ActiveConnections}");

// 广播消息
await connectionManager.BroadcastToUsersAsync(messageData);

// 发送给特定用户
await connectionManager.SendDataToUserAsync(userId, messageData);
```

### 日志记录
```csharp
// 在处理器中记录日志
LogInfo(channel, packet, $"处理数据包: {packet.PacketType}");

// 记录错误
LogError(channel, packet, ex, "处理数据包时发生错误");
```

## 🐛 调试技巧

### 启用详细日志
```csharp
// 在客户端处理器中
pipeline.AddLast(new LoggingHandler("Client", LogLevel.DEBUG));

// 在服务器处理器中
pipeline.AddLast(new LoggingHandler("Server", LogLevel.DEBUG));
```

### 数据包调试
```csharp
// 打印数据包内容
Console.WriteLine($"数据包类型: {packet.PacketType}");
Console.WriteLine($"数据长度: {packet.Data.Length} 字节");
Console.WriteLine($"数据内容: {BitConverter.ToString(packet.Data)}");
```

### 连接调试
```csharp
// 检查连接状态
Console.WriteLine($"连接活跃: {channel.Active}");
Console.WriteLine($"远程地址: {channel.RemoteAddress}");
Console.WriteLine($"本地地址: {channel.LocalAddress}");
```

## 📊 性能监控

### 连接统计
```csharp
var stats = connectionManager.GetStatistics();
Console.WriteLine($"总连接数: {stats.TotalConnections}");
Console.WriteLine($"用户连接数: {stats.UserConnections}");
Console.WriteLine($"活跃连接数: {stats.ActiveConnections}");
```

### 数据包统计
```csharp
// 在数据包处理器中记录统计
public static class PacketStatistics
{
    private static readonly ConcurrentDictionary<PacketType, long> _packetCounts = new();
    
    public static void RecordPacket(PacketType type)
    {
        _packetCounts.AddOrUpdate(type, 1, (key, oldValue) => oldValue + 1);
    }
    
    public static void PrintStatistics()
    {
        foreach (var kvp in _packetCounts)
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value} 个数据包");
        }
    }
}
```

## 🔒 安全最佳实践

### 数据包验证
```csharp
public bool ValidatePacket(IPacket packet)
{
    if (packet == null) return false;
    
    // 检查数据包大小
    var data = packet.GetSerializer().Serialize(packet);
    if (data.Length > MaxPacketSize) return false;
    
    // 检查用户权限
    if (packet is LoginPacket loginPacket)
    {
        return ValidateLoginCredentials(loginPacket);
    }
    
    return true;
}
```

### 连接限制
```csharp
// 限制连接频率
private readonly ConcurrentDictionary<IPEndPoint, DateTime> _connectionAttempts = new();

public bool AllowConnection(IPEndPoint endpoint)
{
    var now = DateTime.UtcNow;
    if (_connectionAttempts.TryGetValue(endpoint, out var lastAttempt))
    {
        if (now - lastAttempt < TimeSpan.FromSeconds(1))
        {
            return false; // 连接过于频繁
        }
    }
    
    _connectionAttempts.AddOrUpdate(endpoint, now, (key, oldValue) => now);
    return true;
}
```

## 📝 代码规范

### 命名约定
- 数据包类: `XxxPacket`
- 序列化器: `XxxPacketSerializer`
- 处理器: `XxxPacketHandler`
- 枚举: `XxxPacketType`

### 文件组织
```
Network/Protocol/
├── ServerBound/
│   ├── XxxPacket.cs
│   └── ServerBoundPacketType.cs
└── ClientBound/
    ├── XxxPacket.cs
    └── ClientBoundPacketType.cs
```

### 注释规范
```csharp
/// <summary>
/// 处理登录数据包
/// </summary>
/// <param name="channel">客户端通道</param>
/// <param name="packet">登录数据包</param>
/// <returns>处理结果</returns>
public async Task<LoginResult> HandleLoginAsync(IChannel channel, LoginPacket packet)
{
    // 实现逻辑
}
```

## 🚨 常见问题

### Q: 数据包序列化失败
**A**: 检查序列化器是否正确注册，数据包类型是否匹配

### Q: 连接断开
**A**: 检查网络连接，服务器是否正常运行，防火墙设置

### Q: 性能问题
**A**: 检查连接池配置，数据包大小，网络延迟

### Q: 内存泄漏
**A**: 确保正确释放资源，使用 `using` 语句，检查事件订阅

## 📚 相关资源

- [DotNetty 文档](https://github.com/Azure/DotNetty)
- [.NET 8 文档](https://docs.microsoft.com/en-us/dotnet/)
- [C# 编程指南](https://docs.microsoft.com/en-us/dotnet/csharp/) 
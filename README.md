# Wrap.Remastered

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

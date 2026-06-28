# LockstepDemo Server

独立 .NET 帧同步中继服务器，供 Unity 客户端联机使用。

## 服务器地址

| 项目 | 值 |
|------|-----|
| 监听地址 | `0.0.0.0:8888`（本机所有网卡） |
| 客户端连接 | `127.0.0.1:8888`（默认，与 `Assets/Scene/Launcher.unity` 中配置一致） |
| 默认最大玩家数 | `3` |
| 协议 | TCP + 长度前缀 Protobuf 包 |

启动成功后终端会输出：

```text
Rogue frame sync server started on 0.0.0.0:8888.
```

## 前置条件

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- 已全局安装 npm（用于统一启动命令）

验证：

```powershell
dotnet --version
npm --version
```

## 启动方式（推荐 npm）

在 `Server` 目录下：

```powershell
cd D:\UnityProject\lockstep\Server
npm start
```

常用命令：

| 命令 | 说明 |
|------|------|
| `npm start` | 启动服务器，端口 `8888`，最多 3 人 |
| `npm run start:single` | 启动服务器，最多 1 人（Agent 自动化 / 单人联机测试） |
| `npm run build` | 仅编译，不运行 |
| `npm run start:exe` | 运行已编译的 exe（需先 `npm run build`） |

在项目根目录也可直接调用：

```powershell
cd D:\UnityProject\lockstep
npm run server
npm run server:single
npm run server:build
```

## 备用：直接使用 dotnet

```powershell
dotnet run --project Server\RogueGameServer\RogueGameServer.csproj -- --port 8888 --max-players 3
```

或运行已编译程序：

```powershell
.\Server\RogueGameServer\bin\Debug\net8.0\RogueGameServer.exe --port 8888 --max-players 3
```

自定义端口或人数：

```powershell
dotnet run --project Server\RogueGameServer\RogueGameServer.csproj -- --port 9000 --max-players 2
```

## 项目结构

```text
Server/
├── package.json              # npm 启动脚本
├── README.md                 # 本文件
└── RogueGameServer/          # .NET 8 控制台项目
    ├── Program.cs
    ├── Network/
    └── README.md             # 协议与 protobuf 说明
```

## 协议说明

详见 [RogueGameServer/README.md](./RogueGameServer/README.md)。

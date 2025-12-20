# 量子隧道（Tunneling）使用说明书

## 1. 工具简介

**量子隧道（Tunneling）** 是一款高效、稳定的内网端口穿透工具，采用反向隧道技术，通过一台公网服务器实现外网安全访问多个内网服务（如远程桌面、Web、SSH、媒体服务器等）。
<img src="https://github.com/moneyfengcn/Tunneling/blob/main/main.jpg?raw=true" />
 
### 架构说明（C/S 模式）

量子隧道（Tunneling）采用经典的 **客户端/服务端（Client/Server）架构**，也称为 **反向隧道(Reverse Tunnel)** 模式，非常适合内网穿透场景。

#### 服务端（Server）
- 部署在具有**公网 IP** 的服务器上（VPS、云服务器或家庭宽带公网主机）。
- 负责监听外网请求、统一管理所有端口映射规则（`MapProxy`）。
- 接收来自内网客户端的主动连接，建立持久隧道通道。
- 将外网对 `PublicPort` 的访问转发到对应客户端的内网服务。



#### 客户端（Client）
- 部署在内网**任意一台**需要暴露服务的主机上（可为 NAS、个人电脑、开发机、树莓派等）。
- **一台客户端**即可穿透该内网所有主机上的所有服务（`MapProxy` 中配置的多个 `LocalPort`）。
- 如需穿透多台内网主机的不同服务，只需在一台主机上运行一个客户端实例
- 客户端主动向服务端发起并维持长连接（心跳保活），无需在内网路由器进行端口映射。

#### 典型部署示例 （仅需要在内网部署一台Tunneling.Client）
- 公网服务器（IP: 8.134.13.229）运行 `Tunneling.Server`
- 内网主机 A（192.168.1.100）运行 `Tunneling.Client` → 暴露 RDP、Web 服务
- 内网主机 B（192.168.1.200）暴露 Jellyfin、SSH 不再需要运行 `Tunneling.Client`
- **所有映射规则统一在服务端的 `appsettings.json` 中配置**

#### 优势总结
- 内网主机无需公网 IP、无需开端口。
- 支持多主机、多服务同时穿透。
- 客户端配置极简，只需填写服务端地址和 AccessToken。
- 安全性高（AccessToken 认证 + 推荐启用 HTTPS）。

**核心原理**：
- 内网客户端主动向公网服务器建立长连接。
- 服务器预先定义好端口映射规则（MapProxy）。
- 客户端连接成功后，服务器即可将外网对公共端口的访问实时转发到对应客户端的内网服务。
- 支持多台内网机器同时接入（每台机器运行一个客户端实例）。

**优势**：
- 配置极简、安全性高（支持 AccessToken 认证）。
- 适合家庭 NAS、自建服务暴露、远程办公、开发调试等场景。

**注意事项**：
- 服务器必须具备公网 IP，防火墙/安全组需放行必要端口。
- 流量全部经服务器中转，请确保服务器可信且安全。

## 2. 系统要求

- **服务器端与客户端**：Windows / Linux / macOS 均支持。
- 程序文件：
  - 服务器端：`Tunneling.Server`（或 `Tunneling.Server.exe`）
  - 客户端：`Tunneling.Client`（或 `Tunneling.Client.exe`）

## 3. 文件组成

发布包包含：
- `Tunneling.Server.exe`（Windows）或 `Tunneling.Server`（Linux/macOS）—— 服务端单文件可执行程序
- `Tunneling.Client.exe`（Windows）或 `Tunneling.Client`（Linux/macOS）—— 客户端单文件可执行程序
- `appsettings.json`（服务端与客户端配置文件模板）
- `README.md`（本说明书）

## 4. 服务端部署与配置

### 4.1 部署步骤
1. 将服务端可执行文件与 `appsettings.json` 放在同一目录。
2. 编辑 `appsettings.json`（见下文）。
3. 确保防火墙/安全组放行 `urls` 中指定的端口以及所有 `MapProxy.PublicPort`。
4. 直接运行可执行文件。

### 4.2 服务端配置文件（appsettings.json）

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "urls": "http://*:1984",
  "SystemConfig": {
    "AccessToken": "A982D360-E59E-4012-B4AF-E571169218AA",
    "UserName": "admin",
    "Password": "123123"
  },
  "MapProxy": [
    {
      "Name": "远程桌面",
      "PublicPort": 2000,
      "LocalHost": "192.168.1.234",
      "LocalPort": 3389
    },
    {
      "Name": "Jellyfin",
      "PublicPort": 8096,
      "LocalHost": "192.168.1.250",
      "LocalPort": 8096
    },
    {
      "Name": "SSH",
      "PublicPort": 10022,
      "LocalHost": "192.168.1.239",
      "LocalPort": 22
    },
    {
      "Name": "MyWeb",
      "PublicPort": 8080,
      "LocalHost": "192.168.1.248",
      "LocalPort": 80
    }
  ]
}
```

### 4.3 MapProxy 配置项表格说明

`MapProxy` 为端口映射规则数组，可添加任意多个条目。每个条目对应一个需要暴露的内网服务。

<table style="width:100%; border-collapse: collapse; margin-bottom: 20px;">
  <thead>
    <tr>
      <th style="border:1px solid #ddd; padding:8px; text-align:left; background-color:#f2f2f2;">字段名</th>
      <th style="border:1px solid #ddd; padding:8px; text-align:left; background-color:#f2f2f2;">说明</th>
      <th style="border:1px solid #ddd; padding:8px; text-align:left; background-color:#f2f2f2;">示例值</th>
      <th style="border:1px solid #ddd; padding:8px; text-align:left; background-color:#f2f2f2;">必填</th>
      <th style="border:1px solid #ddd; padding:8px; text-align:left; background-color:#f2f2f2;">备注</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td style="border:1px solid #ddd; padding:8px;">Name</td>
      <td style="border:1px solid #ddd; padding:8px;">映射名称，仅用于日志和区分不同规则，便于阅读和管理</td>
      <td style="border:1px solid #ddd; padding:8px;">远程桌面</td>
      <td style="border:1px solid #ddd; padding:8px;">是</td>
      <td style="border:1px solid #ddd; padding:8px;">必须唯一，建议填写有意义名称</td>
    </tr>
    <tr>
      <td style="border:1px solid #ddd; padding:8px;">PublicPort</td>
      <td style="border:1px solid #ddd; padding:8px;">外网访问端口（公网用户实际连接的端口）</td>
      <td style="border:1px solid #ddd; padding:8px;">2000</td>
      <td style="border:1px solid #ddd; padding:8px;">是</td>
      <td style="border:1px solid #ddd; padding:8px;">必须唯一，不能与服务端 <code>urls</code> 端口冲突</td>
    </tr>
    <tr>
      <td style="border:1px solid #ddd; padding:8px;">LocalHost</td>
      <td style="border:1px solid #ddd; padding:8px;">内网目标主机 IP（由客户端通道转发，服务端无需直接访问该 IP）</td>
      <td style="border:1px solid #ddd; padding:8px;">192.168.1.234</td>
      <td style="border:1px solid #ddd; padding:8px;">是</td>
      <td style="border:1px solid #ddd; padding:8px;">支持 <code>127.0.0.1</code> 或局域网任意设备 IP</td>
    </tr>
    <tr>
      <td style="border:1px solid #ddd; padding:8px;">LocalPort</td>
      <td style="border:1px solid #ddd; padding:8px;">内网目标服务端口</td>
      <td style="border:1px solid #ddd; padding:8px;">3389</td>
      <td style="border:1px solid #ddd; padding:8px;">是</td>
      <td style="border:1px solid #ddd; padding:8px;">对应服务的实际监听端口（如 RDP 为 3389）</td>
    </tr>
  </tbody>
</table>
<p><strong>示例映射效果</strong>（假设服务端公网 IP 为 8.134.13.229）：</p>

<table style="width:100%; border-collapse: collapse;">
  <thead>
    <tr>
      <th style="border:1px solid #ddd; padding:8px; text-align:left; background-color:#f2f2f2;">映射名称</th>
      <th style="border:1px solid #ddd; padding:8px; text-align:left; background-color:#f2f2f2;">外网访问地址</th>
      <th style="border:1px solid #ddd; padding:8px; text-align:left; background-color:#f2f2f2;">实际转发到的内网服务</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td style="border:1px solid #ddd; padding:8px;">远程桌面</td>
      <td style="border:1px solid #ddd; padding:8px;">8.134.13.229:2000</td>
      <td style="border:1px solid #ddd; padding:8px;">192.168.1.234:3389</td>
    </tr>
    <tr>
      <td style="border:1px solid #ddd; padding:8px;">Jellyfin</td>
      <td style="border:1px solid #ddd; padding:8px;">8.134.13.229:8096</td>
      <td style="border:1px solid #ddd; padding:8px;">192.168.1.250:8096</td>
    </tr>
    <tr>
      <td style="border:1px solid #ddd; padding:8px;">SSH</td>
      <td style="border:1px solid #ddd; padding:8px;">8.134.13.229:10022</td>
      <td style="border:1px solid #ddd; padding:8px;">192.168.1.239:22</td>
    </tr>
    <tr>
      <td style="border:1px solid #ddd; padding:8px;">MyWeb</td>
      <td style="border:1px solid #ddd; padding:8px;">8.134.13.229:8080</td>
      <td style="border:1px solid #ddd; padding:8px;">192.168.1.248:80</td>
    </tr>
  </tbody>
</table>

## 5. 客户端部署与配置（Tunneling.Client）

### 5.1 配置文件（appsettings.json）

放在客户端可执行文件同一目录：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "Server": {
    "ServerAddress": "http://8.134.13.229:1984/",
    "AccessToken": "A982D360-E59E-4012-B4AF-E571169218AA"
  }
}
```
**字段说明：**

- `ServerAddress`：服务端完整地址，必须以 `http://` 或 `https://` 开头，结尾带 `/`。
- `AccessToken`：**必须与服务端完全一致**。

### 5.2 Windows 运行方式

**1. 控制台模式（适合调试）**
```cmd
textTunneling.Client.exe
```
**2. 将客户端安装为 Windows 服务（推荐，开机自启）**
- 以**管理员身份**打开命令提示符或 `PowerShell`，执行：

- **安装服务**：
```cmd
sc create TunnelingClient binPath= "C:\path\to\Tunneling.Client.exe --service" start= auto
```
- **启动服务：**
```cmd
sc start TunnelingClient
```
- **停止服务：**
```cmd
sc stop TunnelingClient
```
- **卸载服务：**
```cmd
sc delete TunnelingClient
```
- **查看状态：**
```cmd
sc query TunnelingClient
```
### 5.3 Linux / macOS 运行方式
```Bash
chmod +x Tunneling.Client
./Tunneling.Client
```
建议使用 `systemd` 或 `nohup` 实现开机自启。
## 6. 使用方式
外网直接通过服务端公网 IP + PublicPort 访问对应内网服务（示例见 4.3 表格）。
## 7. 安全注意事项（必读）

首次部署必须修改：
- 服务端与所有客户端的 `AccessToken` → 复杂随机 GUID
- 服务端 Password → 强密码

所有流量经服务器中转，请确保服务器安全

## 8. 常见问题排查

- 客户端无法连接：检查 `ServerAddress`、端口是否放行、`AccessToken` 是否一致
- 外网访问失败：查看服务端日志确认客户端是否在线
- Windows 服务问题：查看`事件查看器`

祝使用愉快！🚀
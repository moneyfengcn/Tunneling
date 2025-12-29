# Quantum Tunneling (Tunneling)

## 1. Tool Introduction

**Quantum Tunneling (Tunneling)** is an efficient and stable intranet port penetration tool that uses reverse tunnel technology. It enables secure external access to multiple intranet services (such as remote desktop, Web, SSH, media servers, etc.) through a single public server.

### Architecture Description (C/S Mode)

Tunneling adopts the classic **Client/Server (C/S) architecture**, also known as **Reverse Tunnel** mode, which is very suitable for intranet penetration scenarios.








#### Server

- Deployed on a server with a **public IP** (VPS, cloud server, or home broadband public host).
- Responsible for listening to external requests and centrally managing all port mapping rules (`MapProxy`).
- Receives active connections from intranet clients and establishes persistent tunnel channels.
- Forwards external access to `PublicPort` to the corresponding client's intranet service.

#### Client

- Deployed on **any** host in the intranet (NAS, personal computer, development machine, Raspberry Pi, etc.).
- **Each client corresponds to a set of mapping rules (`MapGroup`) via a unique AccessToken**, enabling isolated management of different intranet environments (e.g., home, office).
- A single client can penetrate all configured intranet services within its group (supports multiple hosts and ports).
- To penetrate multiple independent intranet environments, simply run a separate client instance in each environment with a different `AccessToken`.
- The client actively initiates and maintains a long-lived connection (with heartbeat keep-alive) to the server, without requiring port forwarding on the intranet router.

#### Typical Deployment Example (Only need to deploy one Tunneling.Client in the intranet)

- Public server (IP: 8.134.13.229) running `Tunneling.Server`
- Intranet host A (192.168.1.100) running `Tunneling.Client` → Exposes RDP and Web services
- Intranet host B (192.168.1.200) exposing Jellyfin and SSH no longer needs to run `Tunneling.Client`
- **All mapping rules are uniformly configured in the server's `appsettings.json`**

#### Advantages Summary

- Supports simultaneous penetration of multiple intranet environments, multiple hosts, and multiple services, with grouped isolation management.
- Intranet hosts do not require public IP or port opening.
- Client configuration is extremely simple: only need to fill in the server address and corresponding AccessToken.
- High security (AccessToken authentication + recommended HTTPS enablement).

**Core Principle**:

- The intranet client actively establishes a long-lived connection to the public server using a specific `AccessToken`.
- The server pre-defines port mapping rules (`MapProxy`).
- After successful client connection, the server can forward external access to public ports in real-time to the corresponding client's intranet service.
- Supports multiple intranet machines connecting simultaneously (run one client instance per intranet).

**Advantages**:

- Extremely simple configuration and high security (supports `AccessToken` authentication).
- Suitable for home NAS, self-hosted service exposure, remote work, development debugging, and other scenarios.

**Notes**:

- The server must have a public IP, and firewall/security groups must allow necessary ports.
- All traffic is relayed through the server; ensure the server is trusted and secure.

## 2. System Requirements

- **Server and Client**: Supports Windows / Linux / macOS.
- Program Files:
  - Server: `Tunneling.Server` (or `Tunneling.Server.exe`)
  - Client: `Tunneling.Client` (or `Tunneling.Client.exe`)

## 3. File Composition

The release package includes:

- `Tunneling.Server.exe` (Windows) or `Tunneling.Server` (Linux/macOS) — Server single-file executable
- `Tunneling.Client.exe` (Windows) or `Tunneling.Client` (Linux/macOS) — Client single-file executable
- `appsettings.json` (Configuration template for server and client)
- `README.md` (This manual)

## 4. Server Deployment and Configuration

### 4.1 Deployment Steps

1. Place the server executable and `appsettings.json` in the same directory.
2. Edit `appsettings.json` (see below).
3. Ensure firewall/security groups allow the ports specified in `urls` and all `MapProxy.PublicPort`.
4. Run the executable directly.

### 4.2 Server Configuration File (appsettings.json)

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
    "UserName": "admin",
    "Password": "123123",
    "MapGroups": [
      {
        "GroupName": "Home NAS",
        "AccessToken": "A982D360-E59E-4012-B4AF-E571169218AA",
        "MapProxy": [
          {
            "Name": "win2016 Remote Desktop",
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
            "LocalHost": "192.168.1.234",
            "LocalPort": 22
          },
          {
            "Name": "MyWeb",
            "PublicPort": 8080,
            "LocalHost": "192.168.1.234",
            "LocalPort": 80
          }
        ]
      },
      {
        "GroupName": "Office Network",
        "AccessToken": "C795479D-800E-49D9-BC38-A0B91B8A5544",
        "MapProxy": [
          {
            "Name": "win2022 Remote Desktop",
            "PublicPort": 4000,
            "LocalHost": "192.168.1.248",
            "LocalPort": 3389
          },
          {
            "Name": "SQL Server",
            "PublicPort": 21433,
            "LocalHost": "127.0.0.1",
            "LocalPort": 1433
          },
          {
            "Name": "Redis",
            "PublicPort": 26379,
            "LocalHost": "127.0.0.1",
            "LocalPort": 6379
          },
          {
            "Name": "Elasticsearch",
            "PublicPort": 29200,
            "LocalHost": "127.0.0.1",
            "LocalPort": 9200
          },
          {
            "Name": "Coremail",
            "PublicPort": 29000,
            "LocalHost": "127.0.0.1",
            "LocalPort": 9000
          },
          {
            "Name": "ESXi System",
            "PublicPort": 28081,
            "LocalHost": "127.0.0.1",
            "LocalPort": 8081
          },
          {
            "Name": "MySQL",
            "PublicPort": 23306,
            "LocalHost": "127.0.0.1",
            "LocalPort": 3306
          }
        ]
      }
    ]
  }
}
```

### 4.3 MapProxy Configuration Items Table Explanation

`MapGroups` is an array of mapping groups; you can add any number of groups to achieve isolation for different intranet environments.

| Field Name    | Description                                      | Example Value                  | Required | Notes                                      |
|---------------|--------------------------------------------------|--------------------------------|----------|--------------------------------------------|
| GroupName     | Group name for easy management and log distinction | Home NAS                      | Yes     | Suggest meaningful name; must be unique    |
| AccessToken   | Authentication token for the client in this group | A982D360-...                   | Yes     | Must be unique and match client exactly    |
| MapProxy      | Array of port mapping rules for this group        | ...                            | Yes     | Can contain multiple mapping rules        |

`MapProxy` is an array of port mapping rules; you can add any number of entries. Each entry corresponds to one intranet service to expose.

| Field Name   | Description                                                                 | Example Value | Required | Notes                                                                 |
|--------------|-----------------------------------------------------------------------------|---------------|----------|-----------------------------------------------------------------------|
| Name         | Mapping name, used only for logs and distinguishing rules, for readability  | Remote Desktop| Yes     | Must be unique; suggest meaningful name                               |
| PublicPort   | External access port (the port public users actually connect to)            | 2000          | Yes     | Must be unique; cannot conflict with server `urls` port               |
| LocalHost    | Intranet target host IP (forwarded via client channel; server doesn't need direct access) | 192.168.1.234 | Yes     | Supports `127.0.0.1` or any LAN device IP                              |
| LocalPort    | Intranet target service port                                                | 3389          | Yes     | Corresponding to the service's actual listening port (e.g., RDP 3389) |

**Example Mapping Effect** (Assuming server public IP is 8.134.13.229):

| Mapping Name     | External Access Address | Actual Forwarded Intranet Service |
|------------------|-------------------------|-----------------------------------|
| Remote Desktop   | 8.134.13.229:2000       | 192.168.1.234:3389                |
| Jellyfin         | 8.134.13.229:8096       | 192.168.1.250:8096                |
| SSH              | 8.134.13.229:10022      | 192.168.1.239:22                  |
| MyWeb            | 8.134.13.229:8080       | 192.168.1.248:80                  |

## 5. Client Deployment and Configuration (Tunneling.Client)

### 5.1 Configuration File (appsettings.json)

Place in the same directory as the client executable:

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

**Field Explanation**:

- `ServerAddress`: Full server address, must start with `http://` or `https://` and end with `/`.
- `AccessToken`: **Must exactly match the server**.

### 5.2 Windows Running Methods

**1. Console Mode (Suitable for debugging)**

```cmd
Tunneling.Client.exe
```

**2. Install Client as Windows Service (Recommended, auto-start on boot)**

- Open Command Prompt or PowerShell **as Administrator** and execute:

- **Install Service**:

```cmd
sc create TunnelingClient binPath= "C:\path\to\Tunneling.Client.exe --service" start= auto
```

- **Start Service**:

```cmd
sc start TunnelingClient
```

- **Stop Service**:

```cmd
sc stop TunnelingClient
```

- **Uninstall Service**:

```cmd
sc delete TunnelingClient
```

- **Check Status**:

```cmd
sc query TunnelingClient
```

### 5.3 Linux / macOS Running Methods

```bash
chmod +x Tunneling.Client
./Tunneling.Client
```

Recommended to use `systemd` or `nohup` for auto-start on boot.

## 6. Usage

Access the corresponding intranet service directly from external network via server public IP + PublicPort (see examples in 4.3 table).

## 7. Security Notes (Must Read)

For first deployment, must change:

- Server's `UserName` and `Password` → Strong passwords
- Each `MapGroups` `AccessToken` → Complex random GUID, different for each group
- All clients' `AccessToken` must match the corresponding group

All traffic is relayed through the server; ensure server security.

## 8. Common Troubleshooting

- Client cannot connect: Check `ServerAddress`, port allowance, and `AccessToken` consistency
- External access failure: Check server logs to confirm if client is online
- Windows service issues: View Event Viewer

Enjoy using it! 🚀
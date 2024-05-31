# [CS2] Advanced Monitoring
AdvancedMonitoring is a plugin for the CS2
server designed to monitor the server and player states using an HTTP API. The plugin provides information about the current server status, including the player list, through HTTP requests.

# Installation
1. Download the latest release of the plugin from [this link](https://github.com/Armatura-Create/cs2-advancedMonitoring/releases).
2. Extract the downloaded files to **/addons/counterstrikesharp/plugins/**

# Features:
Config File located in **/addons/counterstrikesharp/configs/plugins/AdvancedMonitoring** with Settings:
- custom endpoint request
- interval update data
- show bots
- show hltv

# Config 
**AdvancedMonitoring.json**

``` json
{
    "Endpoint": "monitoring-info",
    "MinIntervalUpdate": 30,
    "ShowBots": true,
    "ShowHLTV": true,
    "Debug": true
}
```

- Endpoint: The access point for HTTP requests.
- MinIntervalUpdate: The minimum interval for server data updates in seconds.
- ShowBots: Whether to show bots in the player list.
- ShowHLTV: Whether to show HLTV players in the player list.
- Debug: Enable debug mode for logging console server.

# Usage

1. Start the CS server with the plugin installed.
2. Make a POST or GET request to the server to get the data:
``` sh
curl -X GET http://server-ip:server-port/monitoring-info/
```
3. Response **json**
``` json
{
    "Name": "Hostname",
    "MapName": "map_name",
    "Port": 27015,
    "IP" : "1.1.1.1",
    "MaxPlayers": 32,
    "TScore": 0,
    "CTScore": 0,
    "Players": [
        {
            "Name": "PlaerName",
            "SteamID64": "123",
            "SteamID32": "123",
            "SteamID2": "STEAM_0:1:123",
            "SteamID3": "[U:1:123]",
            "Kills": 0,
            "Deaths": 0,
            "Assists": 0,
            "Score": 0,
            "Ping": 42,
            "TeamName": "Spectator",
            "PlayTime": 578,
            "IsBot": false,
            "IsHLTV": false,
            "IsSpec": true
        }
    ]
}
```

# Requirements:
[CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v236 or higher
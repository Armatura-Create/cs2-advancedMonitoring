# [CS2] Advanced Monitoring
AdvancedMonitoring is a plugin for the CS2
server designed to monitor the server and player states using an HTTP API. The plugin provides information about the current server status, including the player list, through HTTP requests.

# Installation
1. Download the latest release of the plugin from this link [https://github.com/Armatura-Create/cs2-advancedMonitoring/releases].
2. Extract the downloaded files to **/addons/counterstrikesharp/plugins/**

# Features:
Config File located in **/addons/counterstrikesharp/configs/plugins/AdvancedMonitoring** with Settings:
- custom endpoint request
- interval update data
- show bots
- show hltv

# Config 
**AdvancedMonitoring.json**

{
    "Endpoint": "monitoring-info",
    "MinIntervalUpdate": 30,
    "ShowBots": true,
    "ShowHLTV": true,
    "Debug": true
}

- Endpoint: The access point for HTTP requests.
- MinIntervalUpdate: The minimum interval for server data updates in seconds.
- ShowBots: Whether to show bots in the player list.
- ShowHLTV: Whether to show HLTV players in the player list.
- Debug: Enable debug mode for logging console server.

# Usage

1. Start the CS server with the plugin installed.
2. Make a POST or GET request to the server to get the data:
curl -X GET http://<server-ip>:<server-port>/monitoringInfo/

# Requirements:
[CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v236 or higher
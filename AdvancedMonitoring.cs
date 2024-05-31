using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Cvars;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AdvancedMonitoring
{
    public class AdvancedMonitoring : BasePlugin, IPluginConfig<PluginConfig>
    {
        public override string ModuleName => "AdvancedMonitoring";
        public override string ModuleAuthor => "Armatura";
        public override string ModuleVersion => "0.0.3-alpha";

        public required PluginConfig Config { get; set; }

        private HttpListener? listener = null;
        private CancellationTokenSource? cts = null;
        private ServerDto currentServerData = new ServerDto();
        private Dictionary<CCSPlayerController, int> timePlayers = new Dictionary<CCSPlayerController, int>();
        private readonly object dataLock = new object();

        public void OnConfigParsed(PluginConfig config)
        {
            Config = config;

            if (Config.MinIntervalUpdate < 10)
            {
                Config.MinIntervalUpdate = 10;
            }

            if (string.IsNullOrEmpty(Config.Endpoint))
            {
                Config.Endpoint = "monitoringInfo";
            }

            PrintConsole("Config parsed. \n" +
                $"Endpoint: {Config.Endpoint} \n" +
                $"MinIntervalUpdate: {Config.MinIntervalUpdate} \n" +
                $"ShowBots: {Config.ShowBots} \n" +
                $"ShowHLTV: {Config.ShowHLTV} \n" +
                $"Debug: {Config.Debug}");
        }

        public override void Load(bool hotReload)
        {

            if (!HttpListener.IsSupported) 
            {
                Console.WriteLine("HTTP listener is not supported on this platform.");
                return;
            }

            AddTimer(Config.MinIntervalUpdate, () => UpdateServerData(), TimerFlags.REPEAT);

            RegisterListener<Listeners.OnMapStart>(mapName =>
            {
                var currentPort = ConVar.Find("hostport")!.GetPrimitiveValue<int>();

                currentServerData = new ServerDto
                {
                    Name = ConVar.Find("hostname")!.StringValue,
                    MaxPlayers = Server.MaxPlayers,
                    MapName = Server.MapName,
                    Port = currentPort,
                };
                Server.ExecuteCommand("sv_hibernate_when_empty false");

                StartListener();

                timePlayers.Clear();

                AddTimer(1.0f, () =>
                {
                    lock (dataLock)
                    {
                        var currentPlayers = Utilities.GetPlayers().Where(p => p.Connected == PlayerConnectedState.PlayerConnected && p.AuthorizedSteamID != null).ToList();

                        foreach (var player in currentPlayers)
                        {
                            if (timePlayers.ContainsKey(player))
                            {
                                timePlayers[player]++;
                            }
                            else
                            {
                                timePlayers[player] = 0;
                            }
                        }

                        var playersToRemove = timePlayers.Keys.Where(p => !currentPlayers.Contains(p)).ToList();
                        foreach (var player in playersToRemove)
                        {
                            timePlayers.Remove(player);
                        }
                    }
                }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
            });

            if(hotReload) {
                currentServerData = new ServerDto
                {
                    Name = ConVar.Find("hostname")!.StringValue,
                    MaxPlayers = Server.MaxPlayers,
                    MapName = Server.MapName,
                    Port = currentServerData.Port,
                };
        
                PrintConsole("Hot reload completed. HTTP server stopped.");

                StartListener();
            }
        }

        private void StartListener()
        {

            listener?.Stop();
            listener?.Close();
            cts?.Cancel();

            cts = new CancellationTokenSource();

            listener = new HttpListener();
            listener.Prefixes.Add($"http://*:{currentServerData.Port}/{Config.Endpoint}/");
            listener.Start();

            Task.Run(() => ListenForRequests(cts.Token));
            
            PrintConsole("HTTP server started.");
        }

        private void UpdateServerData()
        {
            try
            {
                lock (dataLock)
                {
                    var playersData = new List<PlayerDto>();

                    foreach (var player in Utilities.GetPlayers().Where(p => p.Connected == PlayerConnectedState.PlayerConnected && p.AuthorizedSteamID != null))
                    {
                        if (player.IsBot && !Config.ShowBots)
                        {
                            continue;
                        }

                        if (player.IsHLTV && !Config.ShowHLTV)
                        {
                            continue;
                        }

                        var result = new PlayerDto
                        {
                            Name = player.PlayerName,
                            SteamID32 = player.AuthorizedSteamID?.SteamId32.ToString(),
                            SteamID64 = player.AuthorizedSteamID?.SteamId64.ToString(),
                            SteamID2 = player.AuthorizedSteamID?.SteamId2.ToString(),
                            SteamID3 = player.AuthorizedSteamID?.SteamId3.ToString(),
                            Kills = player.Kills.Count, //TODO Kills test
                            Deaths = 0, //TODO Deaths
                            Supports = 0, //TODO Supports
                            Ping = player.Ping,
                            TeamName = player.Team.ToString(),
                            PlayTime = timePlayers.ContainsKey(player) ? timePlayers[player] : 0,
                            IsBot = player.IsBot,
                            IsHLTV = player.IsHLTV,
                            IsSpec = player.Team.ToString().Equals("Spectator")
                        };

                        playersData.Add(result);
                    }

                    currentServerData.Players = playersData;
                }

                PrintConsole("Server data updated.");
            }
            catch (Exception ex)
            {
                PrintConsole("Error updating server data: " + ex.Message);
            }
        }

        private async Task ListenForRequests(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (listener == null)
                {
                    PrintConsole("Listener is null.");
                    return;
                }
                try
                {
                    var context = await listener.GetContextAsync();
                    PrintConsole("Request from: " + context.Request.RemoteEndPoint?.Address);
                    ProcessRequest(context);
                }
                catch (Exception ex)
                {
                    PrintConsole("Error receiving request: " + ex.Message);
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            try
            {
                ServerDto serverData;

                lock (dataLock)
                {
                    serverData = currentServerData;
                }

                var responseString = JsonSerializer.Serialize(serverData);
                PrintConsole("Response: " + responseString);

                SendResponse(context, responseString);
            }
            catch (Exception ex)
            {
                PrintConsole("Error processing request: " + ex.Message);
                var errorString = "{\"error\": \"" + ex.Message + "\"}";
                SendResponse(context, errorString);
            }
        }

        private void SendResponse(HttpListenerContext context, string responseString)
        {
            context.Response.ContentType = "application/json";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = Encoding.UTF8.GetByteCount(responseString);

            using var output = context.Response.OutputStream;
            output.Write(Encoding.UTF8.GetBytes(responseString));
            context.Response.Close();
        }

        public override void Unload(bool hotReload)
        {
            cts?.Cancel();
           
            listener?.Stop();
            listener?.Close();
           
            PrintConsole("HTTP server stopped.");
        }

        private void PrintConsole(string message)
        {
            if (Config.Debug)
            {
                Console.WriteLine($"[{ModuleName}] {message}");
            }
        }
    }
}

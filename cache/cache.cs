using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using static AdvancedMonitoring.AdvancedMonitoring;

namespace AdvancedMonitoring;

public class Cache
{
    private ServerDto currentServerData = new();
    private readonly object dataLock = new();
    private readonly Dictionary<CCSPlayerController, int> timePlayers = [];
    private CounterStrikeSharp.API.Modules.Timers.Timer? timeTimer = null;
    private CounterStrikeSharp.API.Modules.Timers.Timer? updateTimer = null;

    public void Init(ServerDto serverData)
    {
        timeTimer?.Kill();
        updateTimer?.Kill();
        lock (dataLock)
        {
            timePlayers.Clear();
            currentServerData = serverData;
        }
        StartUpdateTimer();

        Library.PrintConsole("Cache initialized.");
    }

    public void UpdateRoundEnd(int tscore, int ctscore)
    {
        lock (dataLock)
        {
            currentServerData.TScore = tscore;
            currentServerData.CTScore = ctscore;
        }
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
                    if (player.IsBot && !Instance.Config.ShowBots)
                    {
                        continue;
                    }

                    if (player.IsHLTV && !Instance.Config.ShowHLTV)
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

            Library.PrintConsole("Server data updated.");
        }
        catch (Exception ex)
        {
            Library.PrintConsole("Error updating server data: " + ex.Message);
        }
    }

    private void StartUpdateTimer(){
        timeTimer = Instance.AddTimer(1.0f, () =>
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
        updateTimer = Instance.AddTimer(Instance.Config.MinIntervalUpdate, () => UpdateServerData(), TimerFlags.REPEAT);
    }

    public ServerDto GetCurrentServerData()
    {
        lock (dataLock)
        {
            return currentServerData;
        }
    }

    public void Unload()
    {
        timePlayers.Clear();
        timeTimer?.Kill();
        updateTimer?.Kill();

        Library.PrintConsole("Cache unloaded.");
    }
}
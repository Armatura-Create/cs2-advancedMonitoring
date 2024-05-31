using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using static AdvancedMonitoring.AdvancedMonitoring;

namespace AdvancedMonitoring;

public class Cache
{
    private ServerDto currentServerData = new();
    private readonly object dataLock = new();
    private readonly List<PlayerDto> players = new();
    private CounterStrikeSharp.API.Modules.Timers.Timer? timeTimer = null;
    private CounterStrikeSharp.API.Modules.Timers.Timer? updateTimer = null;

    public void Init(ServerDto serverData)
    {
        timeTimer?.Kill();
        updateTimer?.Kill();
        lock (dataLock)
        {
            var players = currentServerData.Players;
            currentServerData = serverData;

            if (players != null)
            {
                currentServerData.Players = players;
            }
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

    public void UpdateDeath(CCSPlayerController? player)
    {
        if (player == null)
        {
            Library.PrintConsole("UpdateDeath Player is null.");
            return;
        }

        lock (dataLock)
        {
            currentServerData.Players.ForEach(p =>
            {
                if (p.SteamID64 == player.AuthorizedSteamID?.SteamId64.ToString())
                {
                    p.Deaths++;
                }
            });
        }
    }

    public void UpdateAssist(CCSPlayerController? player)
    {
        if (player == null)
        {
            Library.PrintConsole("UpdateAssist Player is null.");
            return;
        }

        lock (dataLock)
        {
            currentServerData.Players.ForEach(p =>
            {
                if (p.SteamID64 == player.AuthorizedSteamID?.SteamId64.ToString())
                {
                    p.Assists++;
                }
            });
        }
    }

    private void StartUpdateTimer(){
        timeTimer = Instance.AddTimer(1.0f, () => UpdateTimePlayers(), TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        updateTimer = Instance.AddTimer(Instance.Config.MinIntervalUpdate, () => UpdateServerData(), TimerFlags.REPEAT);
    }

    private void UpdateTimePlayers()
    {
        try {
            lock (dataLock)
            {
                var currentPlayers = Utilities.GetPlayers().Where(p => p.Connected == PlayerConnectedState.PlayerConnected && p.AuthorizedSteamID != null).ToList();

                foreach (var player in currentPlayers)
                {
                    var p = players.Find(p => p.SteamID64 == player.AuthorizedSteamID?.SteamId64.ToString());

                    if (p != null)
                    {
                        p.PlayTime++;
                    }
                }
            }
        } catch (Exception ex) {
            Library.PrintConsole("Error updating time players: " + ex.Message);
        }
    }

    public void AddPlayer(CCSPlayerController? player)
    {
        if (player == null)
        {
            Library.PrintConsole("AddPlayer Player is null.");
            return;
        }

        lock (dataLock)
        {
            players.Add(new PlayerDto{
                Name = player.PlayerName,
                SteamID32 = player.AuthorizedSteamID?.SteamId32.ToString(),
                SteamID64 = player.AuthorizedSteamID?.SteamId64.ToString(),
                SteamID2 = player.AuthorizedSteamID?.SteamId2.ToString(),
                SteamID3 = player.AuthorizedSteamID?.SteamId3.ToString(),
                Kills = player.Kills.Count,
                Deaths = 0,
                Assists = 0,
                Score = player.Score,
                Ping = player.Ping,
                TeamName = player.Team.ToString(),
                PlayTime = 0,
                IsBot = player.IsBot,
                IsHLTV = player.IsHLTV,
                IsSpec = player.Team.ToString().Equals("Spectator")
            });
        }

        Library.PrintConsole("Player added.");
    }

    public void RemovePlayer(CCSPlayerController? player)
    {
        if (player == null)
        {
            Library.PrintConsole("RemovePlayer Player is null.");
            return;
        }

        lock (dataLock)
        {
            players.RemoveAt(players.FindIndex(p => p.SteamID64 == player.AuthorizedSteamID?.SteamId64.ToString()));
        }

        Library.PrintConsole("Player removed.");
    }

    private void UpdateServerData()
    {
        try
        {
            lock (dataLock)
            {
                foreach (var player in Utilities.GetPlayers().Where(p => p.Connected == PlayerConnectedState.PlayerConnected && p.AuthorizedSteamID != null))
                {
                    var playerData = players.Find(p => p.SteamID64 == player.AuthorizedSteamID?.SteamId64.ToString());

                    if (playerData == null)
                    {
                        AddPlayer(player);
                    }
                    else
                    {
                        playerData.Kills = player.Kills.Count;
                        playerData.Score = player.Score;
                        playerData.Ping = player.Ping;
                        playerData.TeamName = player.Team.ToString();
                    }
                }
            }

            Library.PrintConsole("Server data updated.");
        }
        catch (Exception ex)
        {
            Library.PrintConsole("Error updating server data: " + ex.Message);
        }
    }

    public ServerDto GetCurrentServerData()
    {
        lock (dataLock)
        {
            currentServerData.Players = players;
            return currentServerData;
        }
    }

    public void Unload()
    {
        players.Clear();
        timeTimer?.Kill();
        updateTimer?.Kill();

        Library.PrintConsole("Cache unloaded.");
    }
}
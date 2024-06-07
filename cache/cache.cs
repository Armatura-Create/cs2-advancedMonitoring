using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using static AdvancedMonitoring.AdvancedMonitoring;

namespace AdvancedMonitoring;

public class Cache
{
    private ServerDto currentServerData = new();
    private readonly object dataLock = new();
    private readonly List<PlayerDto> players = [];
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
                if (p.SteamID64 == Library.GetSteamID(player, Library.TypeSteamId.SteamID64))
                {
                    p.Statistic.Deaths++;
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
                if (p.SteamID64 == Library.GetSteamID(player, Library.TypeSteamId.SteamID64))
                {
                    p.Statistic.Assists++;
                }
            });
        }
    }

    public void UpdateKill(CCSPlayerController? player, bool headshot, string weapon)
    {
        if (player == null)
        {
            Library.PrintConsole("UpdateKill Player is null.");
            return;
        }

        lock (dataLock)
        {
            currentServerData.Players.ForEach(p =>
            {
               if (p.SteamID64 == Library.GetSteamID(player, Library.TypeSteamId.SteamID64))
                {
                    p.Statistic.Kills++;
                    if (headshot)
                    {
                        p.Statistic.Headshots++;
                    }
                    if (weapon.Contains("knife"))
                    {
                        p.Statistic.KnifeKills++;
                    }
                }
            });
        }
    }

    public void UpdateCountShoots(CCSPlayerController? player)
    {
        if (player == null)
        {
            Library.PrintConsole("UpdateCountShoots Player is null.");
            return;
        }

        lock (dataLock)
        {
            currentServerData.Players.ForEach(p =>
            {
                if (p.SteamID64 == Library.GetSteamID(player, Library.TypeSteamId.SteamID64))
                {
                    p.Statistic.Shoots++;
                }
            });
        }
    }

    public void UpdateDamage(CCSPlayerController? player, int damage)
    {
        if (player == null)
        {
            Library.PrintConsole("UpdateDamage Player is null.");
            return;
        }

        lock (dataLock)
        {
            currentServerData.Players.ForEach(p =>
            {
                if (p.SteamID64 == Library.GetSteamID(player, Library.TypeSteamId.SteamID64))
                {
                    p.Statistic.Damage += damage;
                }
            });
        }
    }

    private void StartUpdateTimer(){
        timeTimer = Instance.AddTimer(1.0f, UpdateTimePlayers, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
        updateTimer = Instance.AddTimer(Instance.Config.MinIntervalUpdate, UpdateServerData, TimerFlags.REPEAT);
    }

    private void UpdateTimePlayers()
    {
        try {
            lock (dataLock)
            {
                var currentPlayers = Utilities.GetPlayers().Where(p => p.Connected == PlayerConnectedState.PlayerConnected).ToList();

                foreach (var player in currentPlayers)
                {
                    var p = players.Find(p => p.SteamID64 == Library.GetSteamID(player, Library.TypeSteamId.SteamID64));

                    if (p != null)
                    {
                        p.PlayTime++;
                    }
                }

                currentServerData.TimeMap = (long) Math.Round(Server.CurrentTime);
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
            var p = new PlayerDto 
            {
                Name = (player.IsBot ? "[BOT] " : player.IsHLTV ? "[HLTV] " : "") + player.PlayerName,
                SteamID32 = Library.GetSteamID(player, Library.TypeSteamId.SteamID32),
                SteamID64 = Library.GetSteamID(player, Library.TypeSteamId.SteamID64),
                SteamID2 = Library.GetSteamID(player, Library.TypeSteamId.SteamID2),
                SteamID3 = Library.GetSteamID(player, Library.TypeSteamId.SteamID3),
                Ping = player.IsBot || player.IsHLTV? 0 : player.Ping,
                TeamName = player.Team.ToString(),
                PlayTime = 0,
                IsBot = player.IsBot,
                IsHLTV = player.IsHLTV,
                IsSpec = player.Team.ToString().Equals("Spectator")
            };

            p.Statistic.Score = player.Score;

            if (p.SteamID64 != null)
            {
                players.Add(p);
                Library.PrintConsole("Player added.");
            }
        }
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
            var steamID64 = Library.GetSteamID(player, Library.TypeSteamId.SteamID64);
            if (steamID64 != null)
            {
                players.RemoveAt(players.FindIndex(p => p.SteamID64 == steamID64));
            }
        }

        Library.PrintConsole("Player removed.");
    }

    private void UpdateServerData()
    {
        try
        {
            lock (dataLock)
            {
                var currentPlayers = Utilities.GetPlayers().Where(p => p.Connected == PlayerConnectedState.PlayerConnected).ToList();
                foreach (var player in currentPlayers)
                {
                    var playerData = players.Find(p => p.SteamID64 == Library.GetSteamID(player, Library.TypeSteamId.SteamID64));

                    if (playerData == null)
                    {
                        AddPlayer(player);
                    }
                    else
                    {
                        playerData.Statistic.Score = player.Score;
                        playerData.Ping = player.Ping;
                        playerData.TeamName = player.Team.ToString();
                        playerData.IsSpec = player.Team.ToString().Equals("Spectator");
                    }
                }

                //Remove players that disconnected
                players.RemoveAll(p => currentPlayers.All(cp => Library.GetSteamID(cp, Library.TypeSteamId.SteamID64) != p.SteamID64));
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
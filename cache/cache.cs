using AdvancedMonitoring.dto;
using AdvancedMonitoring.library;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using static AdvancedMonitoring.AdvancedMonitoring;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace AdvancedMonitoring.cache
{
    public class Cache
    {
        private ServerDto _currentServerData = new();
        private readonly object _dataLock = new();
        private readonly List<PlayerDto> _players = [];
        private Timer? _timeTimer;
        private Timer? _updateTimer;

        public void Init(ServerDto serverData)
        {
            _timeTimer?.Kill();
            _updateTimer?.Kill();

            lock (_dataLock)
            {
                var oldPlayers = _currentServerData.Players;
                _currentServerData = serverData;

                // Переносим старый список, если он был
                _currentServerData.Players = oldPlayers;
            }

            StartUpdateTimer();
            Library.PrintConsole("Cache initialized.");
        }

        public void UpdateRoundEnd(int tscore, int ctscore)
        {
            lock (_dataLock)
            {
                _currentServerData.TScore = tscore;
                _currentServerData.CTScore = ctscore;
            }
        }

        public void UpdateCache(CCSPlayerController? player, TypeUpdate type)
        {
            UpdateCache(player, type, null);
        }

        public void UpdateCache(CCSPlayerController? player, TypeUpdate type, object? value)
        {
            var steamID64 = Library.GetSteamID(player, Library.TypeSteamId.SteamID64);
            if (steamID64 == null)
            {
                Library.PrintConsole("UpdateCache SteamID64 is null.");
                return;
            }

            lock (_dataLock)
            {
                foreach (var p in _currentServerData.Players)
                {
                    if (p.SteamID64 == steamID64)
                    {
                        switch (type)
                        {
                            case TypeUpdate.Kill:
                                p.Statistic.Kills++;
                                break;
                            case TypeUpdate.KillKnife:
                                p.Statistic.KnifeKills++;
                                break;
                            case TypeUpdate.Assist:
                                p.Statistic.Assists++;
                                break;
                            case TypeUpdate.Death:
                                p.Statistic.Deaths++;
                                break;
                            case TypeUpdate.Shoots:
                                p.Statistic.Shoots++;
                                break;
                            case TypeUpdate.Damage:
                                p.Statistic.Damage += (int)value!;
                                break;
                            case TypeUpdate.Headshot:
                                p.Statistic.Headshots++;
                                break;
                            case TypeUpdate.Time:
                                p.PlayTime++;
                                break;
                        }
                    }
                }
            }
        }

        private void StartUpdateTimer()
        {
            _timeTimer = Instance.AddTimer(1.0f, UpdateTimePlayers, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
            _updateTimer = Instance.AddTimer(Instance.Config.MinIntervalUpdate, UpdateServerData, TimerFlags.REPEAT);
        }

        private void UpdateTimePlayers()
        {
            try
            {
                var currentPlayers = Utilities.GetPlayers()
                    .Where(p => p.Connected == PlayerConnectedState.PlayerConnected)
                    .ToList();

                foreach (var player in currentPlayers)
                {
                    UpdateCache(player, TypeUpdate.Time, null);
                }

                lock (_dataLock)
                {
                    _currentServerData.TimeMap = (long)Math.Round(Server.CurrentTime);
                }
            }
            catch (Exception ex)
            {
                Library.PrintConsole("Error updating time players: " + ex.Message);
            }
        }

        private void UpdateServerData()
        {
            try
            {
                lock (_dataLock)
                {
                    var currentPlayers = Utilities.GetPlayers()
                        .Where(p => p.Connected == PlayerConnectedState.PlayerConnected)
                        .ToList();

                    // Обновляем/добавляем
                    foreach (var player in currentPlayers)
                    {
                        var steamId64 = Library.GetSteamID(player, Library.TypeSteamId.SteamID64);
                        var playerData = _players.Find(p => p.SteamID64 == steamId64);

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

                    // Удаляем тех, кто отключился
                    _players.RemoveAll(p =>
                        currentPlayers.All(cp => Library.GetSteamID(cp, Library.TypeSteamId.SteamID64) != p.SteamID64));
                }

                Library.PrintConsole("Server data updated.");
            }
            catch (Exception ex)
            {
                Library.PrintConsole("Error updating server data: " + ex.Message);
            }
        }

        public void AddPlayer(CCSPlayerController? player)
        {
            if (player == null)
            {
                Library.PrintConsole("AddPlayer Player is null.");
                return;
            }

            lock (_dataLock)
            {
                var steamId64 = Library.GetSteamID(player, Library.TypeSteamId.SteamID64);
                if (steamId64 == null)
                {
                    Library.PrintConsole("AddPlayer SteamID64 is null.");
                    return;
                }

                var p = new PlayerDto
                {
                    Name = (player.IsBot ? "[BOT] " : player.IsHLTV ? "[HLTV] " : "") + player.PlayerName,
                    Slot = player.Slot,
                    SteamID32 = Library.GetSteamID(player, Library.TypeSteamId.SteamID32),
                    SteamID64 = steamId64,
                    SteamID2 = Library.GetSteamID(player, Library.TypeSteamId.SteamID2),
                    SteamID3 = Library.GetSteamID(player, Library.TypeSteamId.SteamID3),
                    Ping = player.IsBot || player.IsHLTV ? 0 : player.Ping,
                    TeamName = player.Team.ToString(),
                    PlayTime = 0,
                    IsBot = player.IsBot,
                    IsHLTV = player.IsHLTV,
                    IsSpec = player.Team.ToString().Equals("Spectator"),
                    Statistic = { Score = player.Score }
                };

                _players.Add(p);
                Library.PrintConsole("Player added.");
            }
        }

        public void RemovePlayer(CCSPlayerController? player)
        {
            lock (_dataLock)
            {
                var steamID64 = Library.GetSteamID(player, Library.TypeSteamId.SteamID64);
                if (steamID64 != null)
                {
                    int idx = _players.FindIndex(p => p.SteamID64 == steamID64);
                    if (idx >= 0)
                    {
                        _players.RemoveAt(idx);
                        Library.PrintConsole("Player removed.");
                    }
                    else
                    {
                        Library.PrintConsole($"RemovePlayer: Not found for {steamID64}.");
                    }
                }
                else
                {
                    Library.PrintConsole("RemovePlayer SteamID64 is null.");
                }
            }
        }

        public ServerDto GetCurrentServerData()
        {
            lock (_dataLock)
            {
                _currentServerData.Players = _players;
                return _currentServerData;
            }
        }

        public void Unload()
        {
            _players.Clear();
            _timeTimer?.Kill();
            _updateTimer?.Kill();

            Library.PrintConsole("Cache unloaded.");
        }
    }

    public enum TypeUpdate
    {
        Kill,
        KillKnife,
        Headshot,
        Assist,
        Death,
        Shoots,
        Damage,
        Time
    }
}
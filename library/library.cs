using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static AdvancedMonitoring.AdvancedMonitoring;

namespace AdvancedMonitoring.library;

public static class Library {
    public static void PrintConsole(string? message)
    {
        if (Instance.Config.Debug)
        {
            Console.WriteLine($"[{Instance.ModuleName}] {message}");
        }
    }

    public static void GetTeamScore(out int tscore, out int ctscore)
    {
        tscore = 0;
        ctscore = 0;

        var csteammanager = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager");

        foreach (var team in csteammanager)
        {
            switch (team.TeamNum)
            {
                case 1: tscore = team.Score; break;
                case 2: ctscore = team.Score; break;
            }
        }
    }

    public static string? GetSteamID(CCSPlayerController? player, TypeSteamId type)
    {
        if (player == null)
        {
            return null;
        }

        if ((player.IsHLTV && Instance.Config.ShowHLTV) || (player.IsBot && Instance.Config.ShowBots))
        {
            return player.IsBot ? "BOT-" + player.Slot : "HLTV-" + player.Slot;
        }
        
        return type switch
        {
            TypeSteamId.SteamID64 => player.AuthorizedSteamID?.SteamId64.ToString(),
            TypeSteamId.SteamID32 => player.AuthorizedSteamID?.SteamId32.ToString(),
            TypeSteamId.SteamID2 => player.AuthorizedSteamID?.SteamId2,
            TypeSteamId.SteamID3 => player.AuthorizedSteamID?.SteamId3,
            _ => null,
        };
    }

    public enum TypeSteamId
    {
        SteamID64,
        SteamID32,
        SteamID2,
        SteamID3
    }
}
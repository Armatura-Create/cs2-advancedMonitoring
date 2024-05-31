using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static AdvancedMonitoring.AdvancedMonitoring;

namespace AdvancedMonitoring;

public static class Library {
    public static void PrintConsole(string message)
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

        foreach (CCSTeam team in csteammanager)
        {
            switch (team.TeamNum)
            {
                case 1: tscore = team.Score; break;
                case 2: ctscore = team.Score; break;
            }
        }
    }
}
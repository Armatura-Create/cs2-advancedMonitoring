using CounterStrikeSharp.API.Core;
using static AdvancedMonitoring.AdvancedMonitoring;

namespace AdvancedMonitoring;

public class Events {

    public void Load()
    {
        Instance.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        Instance.RegisterEventHandler<EventPlayerConnect>(OnPlayerConnected);
        Instance.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnected);
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        Library.GetTeamScore(out int tscore, out int ctscore);

        Instance.Cache.UpdateRoundEnd(tscore, ctscore);

        return HookResult.Continue;
    }

    private HookResult OnPlayerConnected(EventPlayerConnect @event, GameEventInfo info)
    {
        // Instance.Cache.addPlayer(@event.Player);

        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnected(EventPlayerDisconnect @event, GameEventInfo info)
    {
        // Instance.Cache.removePlayer(@event.Player);

        return HookResult.Continue;
    }

    public void Unload()
    {
        Library.PrintConsole("Events unloaded.");
    }
}
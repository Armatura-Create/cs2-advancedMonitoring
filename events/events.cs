using CounterStrikeSharp.API.Core;
using static AdvancedMonitoring.AdvancedMonitoring;

namespace AdvancedMonitoring;

public class Events {

    public void Load()
    {
        Instance.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        Instance.RegisterEventHandler<EventPlayerConnect>(OnPlayerConnected);
        Instance.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnected);
        Instance.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        Library.GetTeamScore(out int tscore, out int ctscore);

        Instance.Cache.UpdateRoundEnd(tscore, ctscore);

        return HookResult.Continue;
    }

    private HookResult OnPlayerConnected(EventPlayerConnect @event, GameEventInfo info)
    {
        Instance.Cache.AddPlayer(@event.Userid);

        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnected(EventPlayerDisconnect @event, GameEventInfo info)
    {
        Instance.Cache.RemovePlayer(@event.Userid);

        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        Instance.Cache.UpdateDeath(@event.Userid);

        if (@event.Attacker != null)
        {
            Instance.Cache.UpdateAssist(@event.Attacker);
        }

        return HookResult.Continue;
    }

    public void Unload()
    {
        Library.PrintConsole("Events unloaded.");
    }
}
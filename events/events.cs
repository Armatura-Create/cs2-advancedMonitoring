using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using static AdvancedMonitoring.AdvancedMonitoring;

namespace AdvancedMonitoring;

public class Events {

    public void Load()
    {
        Instance.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        Instance.RegisterEventHandler<EventPlayerConnect>(OnPlayerConnected);
        Instance.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnected);
        Instance.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        Instance.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        Instance.RegisterEventHandler<EventPlayerShoot>(OnPlayerShoot);
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
        Instance.Cache.UpdateCache(@event.Userid, TypeUpdate.Death, null);
       
        Instance.Cache.UpdateCache(@event.Assister, TypeUpdate.Assist, null);

        if (@event.Attacker != null && @event.Attacker != @event.Userid)
        {
            Instance.Cache.UpdateCache(@event.Attacker, TypeUpdate.Kill, null);
            if (@event.Headshot){
                Instance.Cache.UpdateCache(@event.Attacker, TypeUpdate.Headshot, null);
            }
            if (@event.Weapon == "knife"){
                Instance.Cache.UpdateCache(@event.Attacker, TypeUpdate.KillKnife, null);
            }
        }
        
        return HookResult.Continue;
    }

    private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        CCSPlayerController? victim = @event.Userid;
        CCSPlayerController? attacker = @event.Attacker;

        if (victim == null || attacker == null)
            return HookResult.Continue;

        if (victim == attacker)
            return HookResult.Continue;

        if (!Instance.Config.AccessFriendlyDamage && victim?.Team == attacker?.Team)
            return HookResult.Continue;
            
        Instance.Cache.UpdateCache(attacker, TypeUpdate.Damage, @event.DmgHealth);

        return HookResult.Continue;
    }

    private HookResult OnPlayerShoot(EventPlayerShoot @event, GameEventInfo info)
    {
        Instance.Cache.UpdateCache(@event.Userid, TypeUpdate.Shoots, null);

        return HookResult.Continue;
    }

    public void Unload()
    {
        Library.PrintConsole("Events unloaded.");
    }
}
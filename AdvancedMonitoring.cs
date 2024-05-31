using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;

namespace AdvancedMonitoring; 

public class AdvancedMonitoring : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName => "AdvancedMonitoring";
    public override string ModuleAuthor => "Armatura";
    public override string ModuleVersion => "1.0.0-beta";

    public PluginConfig Config { get; set; } = new();
    public static AdvancedMonitoring Instance { get; set; } = new();
    public Cache Cache { get; set; } = new();
    public HttpSupport HttpSupport { get; set; } = new();
    public Events Events { get; set; } = new();

    public void OnConfigParsed(PluginConfig config)
    {
        Instance = this;
        Config = config;

        if (Config.MinIntervalUpdate < 10)
        {
            Config.MinIntervalUpdate = 10;
        }

        if (string.IsNullOrEmpty(Config.Endpoint))
        {
            Config.Endpoint = "monitoringInfo";
        }

        Library.PrintConsole("Config parsed. \n" +
            $"Endpoint: {Config.Endpoint} \n" +
            $"MinIntervalUpdate: {Config.MinIntervalUpdate} \n" +
            $"ShowBots: {Config.ShowBots} \n" +
            $"ShowHLTV: {Config.ShowHLTV} \n" +
            $"Debug: {Config.Debug}");
    }

    public override void Load(bool hotReload)
    {
        Events.Load();
        RegisterListener<Listeners.OnMapStart>(mapName =>
        {

            Cache.Init(new ServerDto
            {
                Name = ConVar.Find("hostname")!.StringValue,
                MaxPlayers = Server.MaxPlayers,
                MapName = Server.MapName,
                Port = ConVar.Find("hostport")!.GetPrimitiveValue<int>(),
            });

            Server.ExecuteCommand("sv_hibernate_when_empty false");

            HttpSupport.StartHttpListener(Cache.GetCurrentServerData().Port, Config.Endpoint);
        });

        if(hotReload) {
            Cache.Init(new ServerDto
            {
                Name = ConVar.Find("hostname")!.StringValue,
                MaxPlayers = Server.MaxPlayers,
                MapName = Server.MapName,
                Port = ConVar.Find("hostport")!.GetPrimitiveValue<int>(),
            });
    
            Library.PrintConsole("Hot reload completed. HTTP server stopped.");

            HttpSupport.StartHttpListener(Cache.GetCurrentServerData().Port, Config.Endpoint);
        }
    }

    
    public override void Unload(bool hotReload)
    {
        HttpSupport.StopHttpListener();
        Events.Unload();
        Cache.Unload();
    }
}   
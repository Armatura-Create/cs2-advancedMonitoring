using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;

namespace AdvancedMonitoring; 

public class AdvancedMonitoring : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName => "[AdvancedMonitoring]";
    public override string ModuleAuthor => "Armatura";
    public override string ModuleVersion => "1.0.5";

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
            AddTimer(5.0f, () =>
            {
                InitCache();

                Server.ExecuteCommand("sv_hibernate_when_empty false");

                HttpSupport.StartHttpListener(Cache.GetCurrentServerData().Port, Config.Endpoint);

                Console.WriteLine($"{ModuleName} Inited.");
            });            
        });

        if(hotReload) {
            InitCache();

            Library.PrintConsole("Hot reload completed. HTTP server stopped.");

            HttpSupport.StartHttpListener(Cache.GetCurrentServerData().Port, Config.Endpoint);
        }
    }
    
    private void InitCache()
    {
        var hostnameVar = ConVar.Find("hostname");
        var portVar = ConVar.Find("hostport");
        var ipVar = ConVar.Find("ip");

        if (hostnameVar == null || portVar == null || ipVar == null)
        {
            throw new InvalidOperationException("Required ConVars are not initialized.");
        }

        Cache.Init(new ServerDto
        {
            Name = hostnameVar.StringValue,
            MaxPlayers = Server.MaxPlayers,
            MapName = Server.MapName,
            IP = ipVar?.StringValue,
            Port = portVar.GetPrimitiveValue<int>(),
        });
    }
    
    public override void Unload(bool hotReload)
    {
        HttpSupport.StopHttpListener();
        Events.Unload();
        Cache.Unload();
    }
}   
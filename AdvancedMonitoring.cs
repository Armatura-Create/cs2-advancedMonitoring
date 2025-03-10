using AdvancedMonitoring.cache;
using AdvancedMonitoring.dto;
using AdvancedMonitoring.events;
using AdvancedMonitoring.http;
using AdvancedMonitoring.library;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Cvars;

namespace AdvancedMonitoring;

[MinimumApiVersion(305)]
public class AdvancedMonitoring : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName => "[AdvancedMonitoring]";
    public override string ModuleAuthor => "Armatura";
    public override string ModuleVersion => "1.0.7";

    public PluginConfig Config { get; set; } = new();
    public static AdvancedMonitoring Instance { get; set; } = new();
    public Cache Cache { get; set; } = new();
    private HttpSupport HttpSupport { get; set; } = new();
    private Events Events { get; set; } = new();

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
            Config.Endpoint = "monitoring-info";
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
        // Сначала регистрируем события
        Events.Load();

        // Регистрируемся на событие OnMapStart, чтобы чуть позже (AddTimer) запустить слушатель
        RegisterListener<Listeners.OnMapStart>(mapName =>
        {
            AddTimer(5.0f, () =>
            {
                InitCache();

                // Если нужно, можете убрать или оставить эту команду
                Server.ExecuteCommand("sv_hibernate_when_empty false");
                
                var serverData = Cache.GetCurrentServerData();
                var ip = serverData.IP;

                if (ip is null or "0.0.0.0")
                {
                    if (Config.IP is null or "0.0.0.0")
                    {
                        Library.PrintConsole("IP is not set automatically. Set tour IP in config please.");
                        return;
                    }
                    
                    ip = Config.IP;
                }

                // Запускаем HttpListener только один раз
                HttpSupport.StartHttpListener(ip, Cache.GetCurrentServerData().Port, Config.Endpoint);

                Console.WriteLine($"{ModuleName} Inited.");
            });
        });

        // Если это хот-релоад, НЕ запускаем повторно HttpListener прямо здесь,
        // а просто выводим сообщение. Он поднимется после следующего OnMapStart
        if (hotReload)
        {
            Library.PrintConsole("Hot reload completed. The plugin is reloaded (listener will start on next OnMapStart).");
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
        // Останавливаем HttpListener
        HttpSupport.StopHttpListener();
        
        // Выгружаем события и кеш
        Events.Unload();
        Cache.Unload();
    }
}

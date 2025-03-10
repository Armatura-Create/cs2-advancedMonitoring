using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

public class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = "monitoring-info";
    
    [JsonPropertyName("ip")]
    public string IP { get; set; } = "0.0.0.0";

    [JsonPropertyName("min_interval_update")]
    public int MinIntervalUpdate { get; set; } = 30;

    [JsonPropertyName("access_friendly_damage")]
    public bool AccessFriendlyDamage { get; set; } = false;

    [JsonPropertyName("show_bots")]
    public bool ShowBots { get; set; } = false;

    [JsonPropertyName("show_hltv")]
    public bool ShowHLTV { get; set; } = false;

    [JsonPropertyName("debug")]
    public bool Debug { get; set; } = false;
}
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

public class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = "monitoring-info";

    [JsonPropertyName("min_interval_update")]
    public int MinIntervalUpdate { get; set; } = 30;

    [JsonPropertyName("show_bots")]
    public bool ShowBots { get; set; } = false;

    [JsonPropertyName("show_hltv")]
    public bool ShowHLTV { get; set; } = false;

    [JsonPropertyName("debug")]
    public bool Debug { get; set; } = false;
}
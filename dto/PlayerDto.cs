namespace AdvancedMonitoring;

public class PlayerDto
{
    public string? Name { get; set; }
    public int Slot { get; set; }
    public string? SteamID64 { get; set; }
    public string? SteamID32 { get; set; }
    public string? SteamID2 { get; set; }
    public string? SteamID3 { get; set; }
    public StatisticDto Statistic { get; set; } = new();
    public uint? Ping { get; set; }
    public string? TeamName { get; set; }
    public float? PlayTime { get; set; }
    public bool IsBot { get; set; }
    public bool IsHLTV { get; set; }
    public bool IsSpec { get; set; }
}
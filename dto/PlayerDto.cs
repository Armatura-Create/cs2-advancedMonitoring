namespace AdvancedMonitoring;

public class PlayerDto
{
    public string? Name { get; set; }
    public string? SteamID64 { get; set; }
    public string? SteamID32 { get; set; }
    public string? SteamID2 { get; set; }
    public string? SteamID3 { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Supports { get; set; }
    public uint? Ping { get; set; }
    public string? TeamName { get; set; }
    public float? PlayTime { get; set; }
    public bool? IsBot { get; set; }
    public bool? IsHLTV { get; set; }
    public bool? IsSpec { get; set; }
}
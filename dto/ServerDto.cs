namespace AdvancedMonitoring.dto;

public class ServerDto
{
    public string? Name { get; set; }
    public string? MapName { get; set; }
    public int Port { get; set; }
    public string? IP { get; set; }
    public int MaxPlayers { get; set; }
    public long TimeMap { get; set; }
    public int TScore { get; set; }
    public int CTScore { get; set; }

    public List<PlayerDto> Players { get; set; } = [];
}
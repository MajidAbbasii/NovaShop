namespace NovaShop.Common.Models;

public class CacheSettings
{
    public string? Provider { get; set; } = "Memory";
    public string? RedisConnectionString { get; set; }
    public string? InstanceName { get; set; } = "NovaShop_";
}

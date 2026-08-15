namespace NovaShop.Common.Models;

public class RabbitMqSettings
{
    public bool Enabled { get; set; } = false;
    public string? Host { get; set; } = "localhost";
    public string? VirtualHost { get; set; } = "/";
    public string? Username { get; set; } = "guest";
    public string? Password { get; set; } = "guest";
    public string? ProductCreatedQueue { get; set; } = "product-created-queue";
    public string? OrderEventsQueue { get; set; } = "order-events-queue";
}

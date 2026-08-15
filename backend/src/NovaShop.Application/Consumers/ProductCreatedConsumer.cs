using MassTransit;
using NovaShop.Application.Messages;
using Microsoft.Extensions.Logging;

namespace NovaShop.Application.Consumers;

public class ProductCreatedConsumer : IConsumer<ProductCreatedEvent>
{
    private readonly ILogger<ProductCreatedConsumer> _logger;

    public ProductCreatedConsumer(ILogger<ProductCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation("Product Created Event Received: {ProductId} - {Name} - Price: {Price}",
            message.ProductId, message.Name, message.Price);

        // اینجا کارهایی مثل:
        // - ارسال نوتیفیکیشن
        // - بروزرسانی Cache
        // - فراخوانی سرویس دیگر

        await Task.CompletedTask;
    }
}

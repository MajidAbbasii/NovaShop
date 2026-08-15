using MediatR;
using Microsoft.Extensions.Logging;
using NovaShop.Domain.Repositories;

namespace NovaShop.Application.Features.Discounts.Handlers;

public class DeleteDiscountCommandHandler : IRequestHandler<Commands.DeleteDiscountCommand, bool>
{
    private readonly IDiscountRepository _repository;
    private readonly ILogger<DeleteDiscountCommandHandler> _logger;

    public DeleteDiscountCommandHandler(IDiscountRepository repository, ILogger<DeleteDiscountCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> Handle(Commands.DeleteDiscountCommand request, CancellationToken cancellationToken)
    {
        await _repository.DeleteAsync(request.Id);
        _logger.LogInformation("Discount {DiscountId} deleted", request.Id);
        return true;
    }
}

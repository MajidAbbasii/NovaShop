using MediatR;

namespace NovaShop.Application.Features.Categories.Commands;

public record DeleteCategoryCommand(int Id) : IRequest<bool>;

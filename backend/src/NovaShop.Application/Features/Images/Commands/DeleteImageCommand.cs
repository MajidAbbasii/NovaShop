using MediatR;

namespace NovaShop.Application.Features.Images.Commands;

public record DeleteImageCommand(string PublicId) : IRequest<bool>;

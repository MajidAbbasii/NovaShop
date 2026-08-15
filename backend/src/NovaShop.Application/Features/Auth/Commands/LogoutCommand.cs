using MediatR;

public record LogoutCommand(string RefreshToken) : IRequest<bool>;

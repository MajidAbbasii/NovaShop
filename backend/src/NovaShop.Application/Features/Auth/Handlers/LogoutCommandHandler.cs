using MediatR;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, bool>
{
    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Refresh Token revoked: {request.RefreshToken}");
        return true;
    }
}

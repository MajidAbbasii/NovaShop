namespace NovaShop.Domain.Exceptions;

/// <summary>
/// Thrown when there is not enough sellable inventory to satisfy a request.
/// The API middleware maps this to HTTP 409 Conflict with a Persian
/// user-facing message. The exception lives in the Domain layer so that
/// domain entities can throw it without a dependency on the API layer.
/// </summary>
public sealed class InsufficientStockException : Exception
{
    public InsufficientStockException(string productName)
        : base($"محصول \"{productName}\" موجودی کافی ندارد.") { }
}

/// <summary>
/// A controlled business conflict (HTTP 409) — the request was well-formed
/// but cannot be applied due to a non-idempotent business rule violation
/// (e.g. concurrent modification, double state transition).
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

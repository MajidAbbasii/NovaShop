namespace NovaShop.Common.Models;

/// <summary>
/// Authentication feature toggles. Bound from the "Authentication" config section.
/// OTP (SMS one-time code) is the original mobile-first verification path; it can be
/// disabled to allow direct username/password registration + login without SMS.
/// Keep all OTP infrastructure intact so re-enabling is a one-line config change.
/// </summary>
public class AuthenticationOptions
{
    /// <summary>
    /// When true, registration and login may require/use SMS OTP verification.
    /// When false, registration creates the user directly and login uses password only.
    /// </summary>
    public bool OtpEnabled { get; set; } = false;
}

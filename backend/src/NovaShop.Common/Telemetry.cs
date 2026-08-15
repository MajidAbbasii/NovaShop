using System.Diagnostics;

namespace NovaShop.Common;

public static class Telemetry
{
    public static readonly ActivitySource ActivitySource = new("NovaShop", "1.0.0");
}

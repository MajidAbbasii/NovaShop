namespace NovaShop.Common.Models;

/// <summary>Hangfire server + retry configuration (loaded from "Hangfire" section).</summary>
public class HangfireOptions
{
    /// <summary>Number of background worker threads. Null = Hangfire default.</summary>
    public int? WorkerCount { get; set; }

    /// <summary>Maximum automatic retry attempts for a failed job.</summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Optional explicit retry back-off delays (seconds). When empty, Hangfire uses
    /// its default exponential back-off. Provide one entry per attempt to override.
    /// </summary>
    public int[] RetryDelaysInSeconds { get; set; } = Array.Empty<int>();

    /// <summary>Ordered list of queues the server listens on (highest priority first).</summary>
    public string[] Queues { get; set; } = { "critical", "default", "notifications", "sms", "maintenance" };

    /// <summary>
    /// Optional shared access key to open the Hangfire Dashboard without a cookie-based
    /// admin session (e.g. ?hfkey=VALUE or X-Hangfire-Key header). Empty disables the
    /// key and requires a signed-in Admin role instead.
    /// </summary>
    public string DashboardAccessKey { get; set; } = string.Empty;
}

/// <summary>Business thresholds for maintenance jobs (loaded from "Jobs" section).</summary>
public class JobsOptions
{
    /// <summary>Products at or below this stock are flagged as low-stock.</summary>
    public int LowStockThreshold { get; set; } = 5;

    /// <summary>Days a custom-doll request can stay PendingReview before a reminder is sent.</summary>
    public int CustomDollReminderAfterDays { get; set; } = 3;
}

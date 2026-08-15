using Hangfire;
using NovaShop.Application.Jobs;
using NovaShop.Application.Services;

namespace NovaShop.Api.Services;

public class HangfireReservationScheduler : IReservationScheduler
{
    private readonly IBackgroundJobClient _backgroundJobs;

    public HangfireReservationScheduler(IBackgroundJobClient backgroundJobs)
    {
        _backgroundJobs = backgroundJobs;
    }

    public void ScheduleExpiry(int orderId, TimeSpan delay)
    {
        _backgroundJobs.Schedule<ReleaseExpiredReservationsJob>(
            job => job.ReleaseAsync(orderId, CancellationToken.None),
            delay);
    }
}

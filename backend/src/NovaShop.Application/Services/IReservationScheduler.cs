namespace NovaShop.Application.Services;

public interface IReservationScheduler
{
    void ScheduleExpiry(int orderId, TimeSpan delay);
}

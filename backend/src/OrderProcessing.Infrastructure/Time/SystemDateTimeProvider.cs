using OrderProcessing.Application.Common.Interfaces;

namespace OrderProcessing.Infrastructure.Time;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

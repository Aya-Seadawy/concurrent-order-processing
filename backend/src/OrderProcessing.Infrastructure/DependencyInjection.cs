using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderProcessing.Application.Common.Interfaces;
using OrderProcessing.Infrastructure.Notifications;
using OrderProcessing.Infrastructure.Persistence;
using OrderProcessing.Infrastructure.Persistence.Repositories;
using OrderProcessing.Infrastructure.Time;

namespace OrderProcessing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Resolved lazily (inside the options delegate) rather than eagerly here, so that test hosts which
        // inject a connection-string override via ConfigureAppConfiguration after this call still take effect.
        services.AddDbContext<OrderProcessingDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection") ?? "Data Source=orders.db;Default Timeout=5"));

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderNotificationRepository, OrderNotificationRepository>();
        services.AddScoped<IIdempotencyRecordRepository, IdempotencyRecordRepository>();

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.Configure<FakeDeliveryOptions>(configuration.GetSection("Notifications:FakeDelivery"));
        services.AddSingleton<IDeliveryService, FakeDeliveryService>();

        services.Configure<NotificationWorkerOptions>(configuration.GetSection("Notifications:Worker"));
        services.AddSingleton<NotificationDispatcherWorker>();
        services.AddHostedService(sp => sp.GetRequiredService<NotificationDispatcherWorker>());

        return services;
    }
}

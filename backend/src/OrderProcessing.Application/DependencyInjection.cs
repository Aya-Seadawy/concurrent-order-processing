using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OrderProcessing.Application.Common.ChaosHook;
using OrderProcessing.Application.Common.Behaviors;
using OrderProcessing.Application.Common.Idempotency;
using OrderProcessing.Application.Common.Interfaces;

namespace OrderProcessing.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        services.AddSingleton<IIdempotencyPayloadHasher, IdempotencyPayloadHasher>();
        services.AddSingleton<IChaosHook, NoOpChaosHook>();

        return services;
    }
}

using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;


namespace Application
{
    public static class ApplicationDep
    {
        public static IServiceCollection AddApplicationDep(this IServiceCollection services)
        {

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly()));
            services.AddMassTransit(cfg =>
            {
                cfg.UsingRabbitMq((context, bus) =>
                {
                    bus.Host("localhost", "/", h =>
                    {
                        h.Username("guest");
                        h.Password("guest");
                    });
                });
            });

            return services;
        }
    }
}


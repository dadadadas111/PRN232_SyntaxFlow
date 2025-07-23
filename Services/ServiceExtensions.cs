using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Services
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddRedis(this IServiceCollection services, string configuration)
        {
            var muxer = ConnectionMultiplexer.Connect(configuration);
            services.AddSingleton<IConnectionMultiplexer>(muxer);
            services.AddScoped<IRedisService, RedisService>();
            return services;
        }
    }
}

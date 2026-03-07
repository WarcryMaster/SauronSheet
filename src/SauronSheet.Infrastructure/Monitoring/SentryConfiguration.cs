using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Sentry;

namespace SauronSheet.Infrastructure.Monitoring
{
    public static class SentryConfiguration
    {
        public static void AddSauronSheetSentry(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSentry(options =>
            {
                options.Dsn = configuration["Sentry:Dsn"];
                options.TracesSampleRate = 1.0;
                options.BeforeSend = @event =>
                {
                    // Exclude financial PII
                    if (@event.User != null)
                    {
                        @event.User.Id = null;
                        @event.User.Email = null;
                    }
                    @event.SetTag("exclude_pii", "true");
                    return @event;
                };
                options.TracesToIgnore = new[] { "/health" };
            });
        }
    }
}

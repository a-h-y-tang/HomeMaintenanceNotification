using HomeMaintenanceNotification;
using HomeMaintenanceNotification.Connectors;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: FunctionsStartup(typeof(Startup))]
namespace HomeMaintenanceNotification
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
            builder.Services.AddLogging();
            builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
            builder.Services.AddScoped<IAPIConnector, APIConnector>();
            builder.Services.AddScoped<ISendGridConnector, SendGridConnector>();

            // TODO - use keyvault to store SendGrid API key
        }
    }
}

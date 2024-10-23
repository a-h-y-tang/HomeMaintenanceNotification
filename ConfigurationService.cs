#nullable enable
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeMaintenanceNotification
{
    public interface IConfigurationService
    {
        public int GetConfiguration(string configKey, int defaultValue);

        public string GetConfiguration(string configKey, string defaultValue);
    }

    public class ConfigurationService : IConfigurationService
    {
        private readonly IConfiguration _configuration;

        private readonly ILogger? _logger;

        public ConfigurationService(IConfiguration config, ILogger<ConfigurationService>? logger)
        {
            _configuration = config;
            _logger = logger;
        }

        public int GetConfiguration(string configKey, int defaultValue)
        {
            string configValue = _configuration[configKey];
            if (string.IsNullOrWhiteSpace(configValue))
                _logger?.LogError("Configuration not specified {0}", configKey);
            if (int.TryParse(configValue, out int value))
                return value;

            return defaultValue;
        }

        public string GetConfiguration(string configKey, string defaultValue)
        {
            string configValue = _configuration[configKey];
            if (string.IsNullOrWhiteSpace(configValue))
            {
                _logger?.LogError("Configuration not specified {0}", configKey);
                return defaultValue;
            }
            return configValue;
        }
    }    
}

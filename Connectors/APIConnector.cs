using HomeMaintenanceNotification.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Polly;
using Polly.CircuitBreaker;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HomeMaintenanceNotification.Connectors
{
    /// <summary>
    /// Retrieves information from the HomeMaintenance API
    /// </summary>
    public class APIConnector : IAPIConnector
    {
        private readonly IConfiguration _configuration;

        private readonly HttpClient _httpClient;

        private readonly ILogger _logger;

        private readonly ResiliencePipeline _resiliencePipeline;

        private static readonly int DEFAULT_MAX_RETRIES = 2;

        private static readonly int DEFAULT_RETRY_DELAY_MS = 1000;

        public APIConnector(HttpClient httpClient, ILogger<APIConnector> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            // Define a pipeline builder which will be used to compose strategies incrementally.
            int maxRetries = GetConfiguration("APIConnectorMaxRetries", DEFAULT_MAX_RETRIES);
            int retryDelayMs = GetConfiguration("APIConnectorRetryDelayMs", DEFAULT_RETRY_DELAY_MS);
            _resiliencePipeline = RetryCircuitBreakerPipelineBuilder.Build(logger, maxRetries, retryDelayMs);
        }

        public async Task<List<MaintenanceCycleTaskDTO>> GetWeeklyTasks(int weekNumber, CancellationToken cancellationToken = default)
        {
            string url = $"{_configuration["HomeMaintenanceAPIEndpoint"]}/odata/maintenanceCycleTask?$expand=TaskExecutionHistory&$filter=WeekNumber eq {weekNumber}";
            var envelope = await _resiliencePipeline.ExecuteAsync(async ct => {
                return await ExecuteGet(url, cancellationToken);
            }, cancellationToken);

            return envelope.Value;
        }

        public async Task<List<MaintenanceCycleTaskDTO>> GetTasksByFrequencyPeriod(Frequency frequencyPeriod, CancellationToken cancellationToken = default)
        {
            string url = $"{_configuration["HomeMaintenanceAPIEndpoint"]}/odata/maintenanceCycleTask?$expand=TaskExecutionHistory&$filter=TaskFrequency eq {((int)frequencyPeriod)}";
            var envelope = await _resiliencePipeline.ExecuteAsync(async ct => {
                return await ExecuteGet(url, cancellationToken);
            }, cancellationToken);

            return envelope.Value;
        }

        private int GetConfiguration(string configKey, int defaultValue)
        {
            string configValue = _configuration[configKey];
            if (string.IsNullOrWhiteSpace(configValue))
                _logger.LogError("Configuration not specified {0}", configKey);
            if (int.TryParse(configValue, out int value))
                return value;

            return defaultValue;
        }

        private async Task<ODataEnvelope> ExecuteGet(string requestUrl, CancellationToken ct)
        {
            HttpRequestMessage requestMessage = new(HttpMethod.Get, requestUrl);
            //TODO - add call to AAD for a bearer token
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "TODO");

            var result = await _httpClient.SendAsync(requestMessage, ct);
            result.EnsureSuccessStatusCode();
            var contentString = await result.Content.ReadAsStringAsync(ct);

            return JsonSerializer.Deserialize<ODataEnvelope>(contentString,
                new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
        }
    }
}

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

        public APIConnector(HttpClient httpClient, ILogger<APIConnector> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            // Define a pipeline builder which will be used to compose strategies incrementally.
            var pipelineBuilder = new ResiliencePipelineBuilder();

            int maxRetries = GetConfiguration("APIConnectorMaxRetries", 1);
            int retryDelay = GetConfiguration("APIConnectorRetryDelayMs", 100);
            pipelineBuilder.AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not BrokenCircuitException),
                MaxRetryAttempts = maxRetries,
                Delay = TimeSpan.FromMilliseconds(retryDelay),
                OnRetry = args =>
                {
                    var exception = args.Outcome.Exception!;
                    logger.LogError($"Strategy logging: {exception.Message}", exception);
                    return default;
                }
            }); // We are not calling the Build method here because we will do it as a separate step to make the code cleaner.

            pipelineBuilder.AddCircuitBreaker(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                FailureRatio = 1.0,
                SamplingDuration = TimeSpan.FromSeconds(2),
                MinimumThroughput = 2,
                BreakDuration = TimeSpan.FromSeconds(3),
                OnOpened = args =>
                {
                    var exception = args.Outcome.Exception!;
                    logger.LogWarning($"Breaker logging: Breaking the circuit for {args.BreakDuration.TotalMilliseconds}ms! due to: {exception.Message}");
                    return default;
                },
                OnClosed = args =>
                {
                    logger.LogInformation("Breaker logging: Call OK! Closed the circuit again!");
                    return default;
                },
                OnHalfOpened = args =>
                {
                    logger.LogInformation("Breaker logging: Half-open: Next call is a trial!");
                    return default;
                }
            }); // We are not calling the Build method because we want to add one more strategy to the pipeline.

            // Build the pipeline since we have added all the necessary strategies to it.
            _resiliencePipeline = pipelineBuilder.Build();
        }

        public async Task<List<MaintenanceCycleTaskDTO>> GetWeeklyTasks(int weekNumber)
        {

            var contentString = await _resiliencePipeline.ExecuteAsync(async ct => {
                HttpRequestMessage requestMessage = new(HttpMethod.Get, $"{_configuration["HomeMaintenanceAPIEndpoint"]}/odata/maintenanceCycleTask?$expand=TaskExecutionHistory&$filter=WeekNumber eq {weekNumber}");
                //TODO - add call to AAD for a bearer token
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "TODO");
                var result = await _httpClient.SendAsync(requestMessage, ct);
                result.EnsureSuccessStatusCode();
                return await result.Content.ReadAsStringAsync(ct);
            });

            ODataEnvelope envelope = JsonSerializer.Deserialize<ODataEnvelope>(contentString, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true});

            return envelope.Value;
        }

        public async Task<List<MaintenanceCycleTaskDTO>> GetTasksByFrequencyPeriod(Frequency frequencyPeriod)
        {

            var contentString = await _resiliencePipeline.ExecuteAsync(async ct => {
                HttpRequestMessage requestMessage = new(HttpMethod.Get, 
                    $"{_configuration["HomeMaintenanceAPIEndpoint"]}/odata/maintenanceCycleTask?$expand=TaskExecutionHistory&$filter=TaskFrequency eq {((int)frequencyPeriod)}");
                //TODO - add call to AAD for a bearer token
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "TODO");
                var result = await _httpClient.SendAsync(requestMessage, ct);
                result.EnsureSuccessStatusCode();
                return await result.Content.ReadAsStringAsync(ct);
            });
            ODataEnvelope envelope = JsonSerializer.Deserialize<ODataEnvelope>(contentString, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

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
    }
}

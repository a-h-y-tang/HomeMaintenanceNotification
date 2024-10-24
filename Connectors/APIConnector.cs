﻿using HomeMaintenanceNotification.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using System.Collections.Generic;
using System.Net.Http;
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

        public APIConnector(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<List<MaintenanceCycleTaskDTO>> GetWeeklyTasks(int weekNumber)
        {
            HttpRequestMessage requestMessage = new(HttpMethod.Get, $"{_configuration["HomeMaintenanceAPIEndpoint"]}/odata/maintenanceCycleTask?$expand=TaskExecutionHistory&$filter=WeekNumber eq {weekNumber}");
            //TODO - add call to AAD for a bearer token
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "TODO");

            var result = await _httpClient.SendAsync(requestMessage);
            result.EnsureSuccessStatusCode();
            string contentString = await result.Content.ReadAsStringAsync();
            ODataEnvelope envelope = JsonSerializer.Deserialize<ODataEnvelope>(contentString, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true});

            return envelope.Value;
        }

        public async Task<List<MaintenanceCycleTaskDTO>> GetTasksByFrequencyPeriod(Frequency frequencyPeriod)
        {
            HttpRequestMessage requestMessage = new(HttpMethod.Get, $"{_configuration["HomeMaintenanceAPIEndpoint"]}/odata/maintenanceCycleTask?$expand=TaskExecutionHistory&$filter=TaskFrequency eq {((int)frequencyPeriod)}");
            //TODO - add call to AAD for a bearer token
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "TODO");

            var result = await _httpClient.SendAsync(requestMessage);
            result.EnsureSuccessStatusCode();
            string contentString = await result.Content.ReadAsStringAsync();
            ODataEnvelope envelope = JsonSerializer.Deserialize<ODataEnvelope>(contentString, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            return envelope.Value;
        }
    }
}

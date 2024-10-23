using HomeMaintenanceNotification.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HomeMaintenanceNotification.Connectors
{
    /// <summary>
    /// Connector to SendGrid for sending emails
    /// </summary>
    public class SendGridConnector : ISendGridConnector
    {
        private readonly ILogger<SendGridConnector> _logger;

        private readonly IConfigurationService _configService;

        private readonly ResiliencePipeline _resiliencePipeline;

        private static readonly int DEFAULT_MAX_RETRIES = 2;

        private static readonly int DEFAULT_RETRY_DELAY_MS = 10000;

        public SendGridConnector(IConfigurationService configService, ILogger<SendGridConnector> logger)
        {
            _configService = configService;
            _logger = logger;

            int maxRetries = _configService.GetConfiguration("SendGridConnectorMaxRetries", DEFAULT_MAX_RETRIES);
            int retryDelayMs = _configService.GetConfiguration("SendGridConnectorRetryDelayMs", DEFAULT_RETRY_DELAY_MS);
            _resiliencePipeline = RetryCircuitBreakerPipelineBuilder.Build(logger, maxRetries, retryDelayMs);
        }

        /// <summary>
        /// Send email of weekly, quarterly, semi-annual and annual tasks that are due for completion
        /// </summary>
        /// <param name="weeklyTasks"></param>
        /// <param name="quarterlyTasks"></param>
        /// <param name="semiAnnualTasks"></param>
        /// <param name="annualTasks"></param>
        /// <returns></returns>
        public async Task SendEmail(List<MaintenanceCycleTaskDTO> weeklyTasks, List<MaintenanceCycleTaskDTO> quarterlyTasks, List<MaintenanceCycleTaskDTO> semiAnnualTasks, List<MaintenanceCycleTaskDTO> annualTasks, CancellationToken ct = default)
        {
            string fromEmail = _configService.GetConfiguration("fromEmail", string.Empty);
            string fromName = _configService.GetConfiguration("fromName", string.Empty);
            string recipientEmail = _configService.GetConfiguration("recipientEmail", string.Empty);
            string recipientName = _configService.GetConfiguration("recipientName", string.Empty);
            string templateId = _configService.GetConfiguration("templateId", string.Empty);
            string sendGridApiKey = _configService.GetConfiguration("sendGridApiKey", string.Empty);

            SendGridMessage message = new();
            message.SetFrom(fromEmail, fromName);
            message.AddTo(recipientEmail, recipientName);
            message.SetTemplateId(templateId);
            message.SetTemplateData(new 
                {
                    subject = "Home Maintenance Tasks to complete this weekend",
                    weeklyTasks = weeklyTasks,
                    quarterlyTasks = quarterlyTasks,
                    semiAnnualTasks = semiAnnualTasks,
                    annualTasks = annualTasks
                }
            );

            SendGridClient client = new(sendGridApiKey);

            await _resiliencePipeline.ExecuteAsync(async ct =>
            {
                var response = await client.SendEmailAsync(message, ct);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Sent email to {recipientEmail} via SendGrid");
                }
                else
                {
                    _logger.LogError($"Failed to send email to {recipientEmail} via SendGrid");
                }
            }, ct);
        }
    }
}

using HomeMaintenanceNotification.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeMaintenanceNotification.Connectors
{
    /// <summary>
    /// Connector to SendGrid for sending emails
    /// </summary>
    public class SendGridConnector : ISendGridConnector
    {
        private readonly IConfiguration _configuration;

        private readonly ILogger<SendGridConnector> _logger;

        public SendGridConnector(IConfiguration configuration, ILogger<SendGridConnector> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Send email of weekly, quarterly, semi-annual and annual tasks that are due for completion
        /// </summary>
        /// <param name="weeklyTasks"></param>
        /// <param name="quarterlyTasks"></param>
        /// <param name="semiAnnualTasks"></param>
        /// <param name="annualTasks"></param>
        /// <returns></returns>
        public async Task SendEmail(List<MaintenanceCycleTaskDTO> weeklyTasks, List<MaintenanceCycleTaskDTO> quarterlyTasks, List<MaintenanceCycleTaskDTO> semiAnnualTasks, List<MaintenanceCycleTaskDTO> annualTasks)
        {
            string fromEmail = _configuration["fromEmail"];
            string fromName = _configuration["fromName"];
            string recipientEmail = _configuration["recipientEmail"];
            string recipientName = _configuration["recipientName"];
            string templateId = _configuration["templateId"];
            string sendGridApiKey = _configuration["sendGridApiKey"];

            SendGridMessage message = new SendGridMessage();
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
                });

            SendGridClient client = new(sendGridApiKey);
            var response = await client.SendEmailAsync(message);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Sent email to {recipientEmail} via SendGrid");
            }
            else
            {
                _logger.LogError($"Failed to send email to {recipientEmail} via SendGrid");
            }
        }
    }
}

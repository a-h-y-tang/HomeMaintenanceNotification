using System;
using System.Collections.Generic;
using HomeMaintenanceNotification.DTOs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Linq;
using HomeMaintenanceNotification.Connectors;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Mvc;
using System.Web.Http;
using Microsoft.AspNetCore.Http;

namespace HomeMaintenanceNotification
{
    /// <summary>
    /// Triggered each weekend morning to calculate which home maintenance jobs are to be done this weekend
    /// </summary>
    public class NotifyIncompleteJobsForWeekendFunction
    {
        private readonly IAPIConnector _connector;

        private readonly ILogger _logger;

        public NotifyIncompleteJobsForWeekendFunction(IAPIConnector connector, ILogger<NotifyIncompleteJobsForWeekendFunction> logger)
        {
            _connector = connector;
            _logger = logger;
        }

        [FunctionName("NotifyIncompleteJobsForWeekendByHTTP")]
        public async Task<IActionResult> Post([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "notifyTasks")] HttpRequest req)
        {
            try
            {
                await Execute();
                return new OkResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing home maintenance notifications");
                return new InternalServerErrorResult();
            }
        }


        [FunctionName("NotifyIncompleteJobsForWeekendByTimer")]
        public async Task Run([TimerTrigger("0 0 7 * * 6")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"NotifyIncompleteJobsForWeekend function executed at: {DateTime.Now}");

            await Execute();
        }

        private async Task Execute()
        {            
            // calculate the week that today is.  week 0 doesn't exist.  the first saturday is week 1.
            int dayOfMonth = DateTime.Now.Day;
            int weekOfMonth = (dayOfMonth / 7) + 1;

            // call home maintenance API for jobs for this weekend
            List<MaintenanceCycleTaskDTO> weeklyTasks = await _connector.GetWeeklyTasks(weekOfMonth);

            // call home maintenance API for quarterly jobs that haven't been done in 3 months
            List<MaintenanceCycleTaskDTO> quarterlyTasks = await _connector.GetTasksByFrequencyPeriod(Frequency.Quarterly);

            // call home maintenance API for semi-annual jobs that haven't been done in 6 months
            List<MaintenanceCycleTaskDTO> semiAnnualTasks = await _connector.GetTasksByFrequencyPeriod(Frequency.Semiannual);

            // call home maintenance API for annual jobs that haven't been done in the last 12 months
            List<MaintenanceCycleTaskDTO> yearlyTasks = await _connector.GetTasksByFrequencyPeriod(Frequency.Yearly);

            // filter out the jobs that have already been done in the last couple of weeks (sometimes jobs get done in a different order to the normal week due to weather)
            weeklyTasks.RemoveAll(task =>
                {
                    TaskExecutionHistoryDTO lastExecutionRecord = task.TaskExecutionHistory.OrderByDescending(task => task.TaskExecutionDateTime).FirstOrDefault();
                    return (lastExecutionRecord != null && DateTime.Now.Subtract(lastExecutionRecord.TaskExecutionDateTime).Days < 16);
                }
            );

            // construct and send email to SendGrid
            // TODO - SendEmail(weeklyTasks, quarterlyTasks, semiAnnualTasks, yearlyTasks);
        }
    }
}

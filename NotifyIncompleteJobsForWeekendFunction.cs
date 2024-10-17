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
using System.Runtime.ConstrainedExecution;

namespace HomeMaintenanceNotification
{
    /// <summary>
    /// Notifies task manager of home maintenance tasks to complete
    /// </summary>
    public class NotifyIncompleteJobsForWeekendFunction
    {
        private readonly IAPIConnector _apiConnector;

        private readonly ISendGridConnector _sendGridConnector;

        private readonly ILogger _logger;

        private static int WEEKLY_DAYS_CHECK = 16;
        private static int QUARTERLY_DAYS_CHECK = 92;
        private static int SEMIANNUAL_DAYS_CHECK = 183;
        private static int ANNUAL_DAYS_CHECK = 365;

        public NotifyIncompleteJobsForWeekendFunction(IAPIConnector apiConnector, 
            ISendGridConnector sendGridConnector,
            ILogger<NotifyIncompleteJobsForWeekendFunction> logger)
        {
            _apiConnector = apiConnector;
            _sendGridConnector = sendGridConnector;
            _logger = logger;
        }

        /// <summary>
        /// Manual trigger for testing or adhoc use
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [FunctionName("NotifyIncompleteJobsForWeekendByHTTP")]
        public async Task<IActionResult> Post([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "notifyTasks")] HttpRequest req)
        {
            try
            {
                _logger.LogInformation($"NotifyIncompleteJobsForWeekendByHttp function executed at: {DateTime.Now}");

                await Execute();
                return new OkResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing home maintenance notifications");
                return new InternalServerErrorResult();
            }
        }

        /// <summary>
        /// Triggered each weekend morning to calculate which home maintenance jobs are to be done this weekend
        /// </summary>
        /// <param name="myTimer"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("NotifyIncompleteJobsForWeekendByTimer")]
        public async Task Run([TimerTrigger("0 0 7 * * 6")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"NotifyIncompleteJobsForWeekendByTimer function executed at: {DateTime.Now}");

            await Execute();
        }

        private async Task Execute()
        {            
            // calculate the week that today is.  week 0 doesn't exist.  the first saturday is week 1.
            int dayOfMonth = DateTime.Now.Day;
            int weekOfMonth = (dayOfMonth / 7) + 1;

            // call home maintenance API for jobs for this weekend
            _logger.LogInformation("Retrieving weekly tasks for week: {0}", weekOfMonth);
            List<MaintenanceCycleTaskDTO> weeklyTasks = await _apiConnector.GetWeeklyTasks(weekOfMonth);

            // call home maintenance API for quarterly jobs that haven't been done in 3 months
            List<MaintenanceCycleTaskDTO> quarterlyTasks = await _apiConnector.GetTasksByFrequencyPeriod(Frequency.Quarterly);

            // call home maintenance API for semi-annual jobs that haven't been done in 6 months
            List<MaintenanceCycleTaskDTO> semiAnnualTasks = await _apiConnector.GetTasksByFrequencyPeriod(Frequency.Semiannual);

            // call home maintenance API for annual jobs that haven't been done in the last 12 months
            List<MaintenanceCycleTaskDTO> yearlyTasks = await _apiConnector.GetTasksByFrequencyPeriod(Frequency.Yearly);

            // filter out the jobs that have already been done in the last couple of weeks (sometimes jobs get done in a different order to the normal week due to weather)
            removeCompletedTasks(weeklyTasks, WEEKLY_DAYS_CHECK);
            removeCompletedTasks(quarterlyTasks, QUARTERLY_DAYS_CHECK);
            removeCompletedTasks(semiAnnualTasks, SEMIANNUAL_DAYS_CHECK);
            removeCompletedTasks(yearlyTasks, ANNUAL_DAYS_CHECK);

            // construct and send email to SendGrid
            await _sendGridConnector.SendEmail(weeklyTasks, quarterlyTasks, semiAnnualTasks, yearlyTasks);
        }

        private void removeCompletedTasks(List<MaintenanceCycleTaskDTO> tasks, int withinDaysToCheck)
        {
            tasks.RemoveAll(task =>
                {
                    TaskExecutionHistoryDTO lastExecutionRecord = task.TaskExecutionHistory.OrderByDescending(task => task.TaskExecutionDateTime).FirstOrDefault();
                    return (lastExecutionRecord != null && DateTime.Now.Subtract(lastExecutionRecord.TaskExecutionDateTime).Days < withinDaysToCheck);
                }
            );
        }
    }
}

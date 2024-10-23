using HomeMaintenanceNotification.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HomeMaintenanceNotification.Connectors
{
    public interface ISendGridConnector
    {
        public Task SendEmail(List<MaintenanceCycleTaskDTO> weeklyTasks,
            List<MaintenanceCycleTaskDTO> quarterlyTasks,
            List<MaintenanceCycleTaskDTO> semiAnnualTasks,
            List<MaintenanceCycleTaskDTO> annualTasks,
            CancellationToken cancellationToken);
    }
}

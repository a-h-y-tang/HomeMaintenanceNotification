using HomeMaintenanceNotification.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HomeMaintenanceNotification.Connectors
{
    public interface IAPIConnector
    {
        public Task<List<MaintenanceCycleTaskDTO>> GetWeeklyTasks(int weekNumber, CancellationToken ct);

        public Task<List<MaintenanceCycleTaskDTO>> GetTasksByFrequencyPeriod(Frequency frequencyPeriod, CancellationToken ct);
    }
}

using System.Collections.Generic;

namespace HomeMaintenanceNotification.DTOs
{
    /// <summary>
    /// Data transfer object for maintenance cycle task
    /// </summary>
    public class MaintenanceCycleTaskDTO
    {
        public long? Id { get; set; }

        public string TaskName { get; set; }

        public int TaskFrequency { get; set; }

        public int? WeekNumber { get; set; }

        public IEnumerable<TaskExecutionHistoryDTO>? TaskExecutionHistory { get; set; }
    }

    public enum Frequency : int { Monthly = 0, Quarterly = 1, Semiannual = 2, Yearly = 3 }
}

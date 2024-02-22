using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeMaintenanceNotification.DTOs
{
    /// <summary>
    /// Task Execution History EF entity
    /// </summary>
    public class TaskExecutionHistoryDTO
    {
        /// <summary>
        /// Surrogate priamry key for the task execution history
        /// </summary>
        [Key]
        [Column(name: "TaskExecutionKey")]
        public long TaskExecutionKey { get; set; }

        /// <summary>
        /// Foreign key to the MaintenanceCycle entity.  
        /// All executed tasks are related to exactly one maintenance cycle task record
        /// </summary>
        [Column(name: "TaskKey")]
        public long TaskKey { get; set; }

        /// <summary>
        /// Date/time the task is executed
        /// </summary>
        [Column(name: "TaskExecutionDateTime")]
        public DateTime TaskExecutionDateTime { get; set; }

        /// <summary>
        /// Note left by the person performing the task
        /// </summary>
        [Column(name: "TaskNote")]
        public string TaskNote { get; set; }

        /// <summary>
        /// Name of the person who performed the task
        /// </summary>
        [Column(name: "TaskPerformedBy")]
        public string TaskPerformedBy { get; set; }

        /// <summary>
        /// Optimistic locking field
        /// </summary>
        [Column(name: "RowVersion")]
        public int RowVersion { get; set; }
    }    
}

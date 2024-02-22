using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeMaintenanceNotification.DTOs
{
    public class ODataEnvelope
    {
        public List<MaintenanceCycleTaskDTO> Value { get; set; }
    }
}

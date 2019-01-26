using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoLocal.TimeTracker.Dashboard.Interfaces
{
    public interface ITimerHoursService
    {
        Task createTimerHoursAsync(string requestId = "");
        Task UpdateTimerHoursAsync(string requestId = "");
        Task<bool> GetStartStopButtonStatusAsync(string requestId = "");
    }
}

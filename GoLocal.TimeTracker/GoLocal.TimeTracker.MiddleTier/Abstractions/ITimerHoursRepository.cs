using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GoLocal.TimeTracker.MiddleTier.Abstractions
{
    public interface ITimerHoursRepository
    {
        Task<string> CreateTimerHoursAsync(DateTimeOffset dateTime, string requestId = "");
        Task UpdateTimerHoursAsync(DateTimeOffset dateTime, string requestId = "");
        Task<bool> GetStartStopButtonStatusAsync(string requestId = "");
    }
}

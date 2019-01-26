using GoLocal.TimeTracker.Dashboard.Interfaces;
using GoLocal.TimeTracker.MiddleTier.Abstractions;
using GoLocal.TimeTracker.MiddleTier.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoLocal.TimeTracker.Dashboard.Services
{
    public class TimerHoursService : ITimerHoursService
    {
        private readonly ILogger _logger;
        private readonly ITimerHoursRepository _timerhoursRepository;
        private readonly TimezoneHelper _timezoneHelper;

        public TimerHoursService(
            ILogger<TimerHoursService> logger,
            ITimerHoursRepository timerHoursRepository,
            TimezoneHelper timezoneHelper)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timerhoursRepository = timerHoursRepository ?? throw new ArgumentNullException(nameof(timerHoursRepository));
            _timezoneHelper = timezoneHelper ?? throw new ArgumentNullException(nameof(timezoneHelper));
        }

        public async Task createTimerHoursAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - TimerHoursService_CreateTimerHoursAsync called.");

            try
            {
                // Initialize the Time Zone based on appSettings value.
                var definedZone = _timezoneHelper.timeZoneInfo;
                DateTime startDateTime = TimeZoneInfo.ConvertTime(DateTime.Now, definedZone);
                await _timerhoursRepository.CreateTimerHoursAsync(startDateTime,requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - TimerHoursService_CreateTimerHoursAsync Service Exception: {ex}");
                throw;
            }
        }

        public async Task<bool> GetStartStopButtonStatusAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - TimerHoursService_GetStartStopButtonStatusAsync called.");

            try
            {
                bool status = await _timerhoursRepository.GetStartStopButtonStatusAsync(requestId);
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - TimerHoursService_GetStartStopButtonStatusAsync Service Exception: {ex}");
                throw;
            }
        }

        public async Task UpdateTimerHoursAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - TimerHoursService_UpdateTimerHoursAsync called.");

            try
            {
                // Initialize the Time Zone based on appSettings value.
                var definedZone = _timezoneHelper.timeZoneInfo;
                DateTime stopDateTime = TimeZoneInfo.ConvertTime(DateTime.Now,definedZone);
                await _timerhoursRepository.UpdateTimerHoursAsync(stopDateTime, requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - TimerHoursService_UpdateTimerHoursAsync Service Exception: {ex}");
                throw;
            }
        }
    }
}

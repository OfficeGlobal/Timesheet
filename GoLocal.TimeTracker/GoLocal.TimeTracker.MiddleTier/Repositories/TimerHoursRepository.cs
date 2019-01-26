using GoLocal.TimeTracker.MiddleTier.Abstractions;
using GoLocal.TimeTracker.MiddleTier.Extensions;
using GoLocal.TimeTracker.MiddleTier.Helpers;
using GoLocal.TimeTracker.MiddleTier.Models;
using GoLocal.TimeTracker.MiddleTier.Services.AppContext;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GoLocal.TimeTracker.MiddleTier.Repositories
{
    public class TimerHoursRepository : ITimerHoursRepository
    {
        private readonly ILogger _logger;
        private readonly TimeTrackerOptions _timeTrackerOptions;
        private readonly GraphAppSharePointService _graphSharePointService;
        private readonly IUserContext _userContext;

        public TimerHoursRepository(
            ILogger<TimerHoursRepository> logger,
            IOptions<TimeTrackerOptions> timeTrackerOptions,
            GraphAppSharePointService graphSharePointService,
            IUserContext userContext
            )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timeTrackerOptions = timeTrackerOptions.Value ?? throw new ArgumentNullException(nameof(timeTrackerOptions));
            _graphSharePointService = graphSharePointService ?? throw new ArgumentNullException(nameof(graphSharePointService));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }
        public async Task<string> CreateTimerHoursAsync(DateTimeOffset dateTime, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - TimerHourRepository_CreateTimerHoursAsync called.");
            try
            {
                // Create JSON object
                dynamic fieldsObject = new JObject();
                fieldsObject.Date = dateTime.ToString("MM/dd/yyyy");
                fieldsObject.StartTime = dateTime.ToString("h:mm tt");
                fieldsObject.StopTime = "InProgress";
                dynamic jsonObject = new JObject();
                jsonObject.fields = fieldsObject;

                // Get the site list
                var userObjectIdentifier = _userContext.User.FindFirst(AzureAdAuthenticationBuilderExtensions.ObjectIdentifierType)?.Value;
                var siteList = await _graphSharePointService.GetSiteListAsync(userObjectIdentifier, ListSchema.TimerHoursSchema);

                // Call graph to create the item in the SHarePoint List
                var entryId = await _graphSharePointService.CreateSiteListItemAsync(siteList, jsonObject.ToString());
                return entryId;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - TimerHoursRepository_CreateTimerHoursAsync Service Exception: {ex}");
                throw;
            }
        }

        public async Task<bool> GetStartStopButtonStatusAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - TimerHourRepository_GetStartStopButtonStatusAsync called.");
            try
            {
                var lastEntry = await GetLastEntry();
                bool status = true;
                if (lastEntry.Count == 1)
                    status = false;
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - TimerHoursRepository_GetStartStopButtonStatusAsync Service Exception: {ex}");
                throw;
            }
        }

        public async Task UpdateTimerHoursAsync(DateTimeOffset dateTime, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - TimerHourRepository_UpdateTimerHoursAsync called.");
            try
            {
                // Create JSON object
                dynamic fieldsObject = new JObject();
                fieldsObject.StopTime = dateTime.ToString("h:mm tt");
                dynamic jsonObject = new JObject();
                jsonObject.fields = fieldsObject;

                var lastEntry = await GetLastEntry();
                string id = string.Empty;
                if (lastEntry.Count == 1)
                     id = lastEntry[0].Id.ToString();
                
                // Get the site list
                var userObjectIdentifier = _userContext.User.FindFirst(AzureAdAuthenticationBuilderExtensions.ObjectIdentifierType)?.Value;
                var siteList = await _graphSharePointService.GetSiteListAsync(userObjectIdentifier, ListSchema.TimerHoursSchema);
                // Call graph to create the item in the SHarePoint List
                await _graphSharePointService.UpdateSiteListItemAsync(siteList, id, jsonObject.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - TimerHoursRepository_UpdateTimerHoursAsync Service Exception: {ex}");
                throw;
            }
        }

        private async Task<List<GraphResultItem>> GetLastEntry()
        {
            // Get the site list
            var userObjectIdentifier = _userContext.User.FindFirst(AzureAdAuthenticationBuilderExtensions.ObjectIdentifierType)?.Value;
            var siteList = await _graphSharePointService.GetSiteListAsync(userObjectIdentifier, ListSchema.TimerHoursSchema);

            var queryOptions = new List<QueryOption>();
            queryOptions.Add(new QueryOption("filter", @"startswith(fields/StopTime,'InProgress')"));
            // Call graph to get all registered users
            return await _graphSharePointService.GetSiteListItemsAsync(siteList, queryOptions);
        }
    }
}

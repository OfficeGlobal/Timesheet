// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GoLocal.TimeWise.AzureFunctions.Abstractions;
using GoLocal.TimeWise.AzureFunctions.Helpers;
using GoLocal.TimeWise.AzureFunctions.Models;
using GoLocal.TimeWise.AzureFunctions.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.WebJobs;


namespace GoLocal.TimeWise.AzureFunctions.Services
{
    public class RefreshHoursTask
    {
        private readonly TimeTrackerOptions _timeTrackerOptions;
        private readonly GraphAppUserService _graphUserService;
        private readonly GraphAppSharePointService _graphSharePointService;
        private SiteList _usersSiteList;
        private Microsoft.Azure.WebJobs.ExecutionContext _context;

        public RefreshHoursTask(
            TimeTrackerOptions timeTrackerOptions,
            GraphAppUserService graphUserService,
            GraphAppSharePointService graphSharePointService,
            Microsoft.Azure.WebJobs.ExecutionContext context)
        {
            _timeTrackerOptions = timeTrackerOptions ?? throw new ArgumentNullException(nameof(timeTrackerOptions));
            _graphUserService = graphUserService ?? throw new ArgumentNullException(nameof(graphUserService));
            _graphSharePointService = graphSharePointService ?? throw new ArgumentNullException(nameof(graphSharePointService));
            _context = context ?? throw new ArgumentNullException(nameof(context));

        }

        public async Task ExecuteAsync()
        {
            if (_usersSiteList == null) await TryGetSiteList();

            await refreshHoursTask();
        }

        private async Task TryGetSiteList()
        {
            _usersSiteList = await _graphSharePointService.GetSiteListAsync("users", ListSchema.UsersListSchema);
        }

        public async Task<string> refreshHoursTask()
        {
            var queryOptions = new List<QueryOption>();
            queryOptions.Add(new QueryOption("filter", @"startswith(fields/DailyUpdateStatus,'scheduled')"));

            // Call graph to get all registered users
            var usersList = await _graphSharePointService.GetSiteListItemsAsync(_usersSiteList, queryOptions);

            var date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 00, 00, 00, 000);
            HelperMethods helperMethods = new HelperMethods(_context);

            if (usersList?.Count > 0)
            {
                foreach (var item in usersList)
                {
                    try
                    {
                        updateStatus("inprogress", item.Id);
                        var userObjectIdentifier = item.Properties["ObjectIdentifier"].ToString();
                        var workHrsRepo = helperMethods.GetWorkHoursRepository(userObjectIdentifier);
                        var workHoursList = await workHrsRepo.GetItemsAsync(DateTime.Now);

                        //calculate timer hours if the timer is enabled
                        if (_timeTrackerOptions.EnableTimer == true)
                        {
                            var dateEntry = from x in workHoursList
                                            where x.Fields.Date.ToString() == DateTime.Now.AddDays(-1).ToString("yyyyMMdd")
                                            select x;
                            string id = dateEntry.ToArray()[0].Id;
                            await UpdateTimerHours(id, date.AddDays(-1), userObjectIdentifier);
                        }

                        updateStatus("updated", item.Id);
                    }
                    catch(Exception ex)
                    {
                        updateStatus("scheduled", item.Id);
                    }

                }
            }
                    return "Data Refresh Is Complete";
        }
        public async Task UpdateTimerHours(string id, DateTime date, string userObjectIdentifier)
        {
            try
            {
                //get timer hours list.
                var timerHoursList = new SiteList
                {
                    SiteId = _timeTrackerOptions.SharePointSiteId,
                    ListId = _timeTrackerOptions.TimerHoursListPrefix + userObjectIdentifier
                };
                //get all timer entries from userTimerHoursList
                var queryOptions = new List<QueryOption>();
                queryOptions.Add(new QueryOption("filter", @"startswith(fields/Date, '" + date.ToString("MM/dd/yyyy") + "')"));
                var timerHours = await _graphSharePointService.GetSiteListItemsAsync(timerHoursList, queryOptions);

                HelperMethods helperMethods = new HelperMethods(_context);
                //get email timer 
                var emailHours = await helperMethods.GetGraphAppMailService(userObjectIdentifier).GetMailItemsAsync(date, date.AddDays(1).AddSeconds(-1));
                //get calender hours 
                var calenderHours = await helperMethods.GetGraphAppCalendarService(userObjectIdentifier).GetCalendarEventsAsync(date, date.AddDays(1).AddSeconds(-1));

                //variables declared to keep track of the total timer hours info.
                int calendarTimerHours = 0;
                int calendarTimerMinutes = 0;
                int emailTimerHours = 0;
                int emailTimerMinutes = 0;
                int otherTimerHours = 0;
                int otherTimerMinutes = 0;
                TimeSpan totalTimerHours = new TimeSpan();
                string stopTime = string.Empty;

                // Calculate timer hours
                if (timerHours != null)
                {
                    foreach (var timerSet in timerHours)
                    {
                        //handling the case when the user doent click the stop button.
                        if (timerSet.Properties["StopTime"].ToString() == "InProgress")
                        {
                            stopTime = "11:59 PM";
                            //update stopTime in the list
                            dynamic stopTimeField = new JObject();
                            stopTimeField.StopTime = stopTime;
                            dynamic stopTimeRootObj = new JObject();
                            stopTimeRootObj.fields = stopTimeField;
                            await _graphSharePointService.UpdateSiteListItemAsync(timerHoursList, timerSet.Id, stopTimeRootObj.ToString());
                        }
                        else
                        {
                            stopTime = timerSet.Properties["StopTime"].ToString();
                        }

                        //get how long the start button was clicked before the user clicked stop.
                        totalTimerHours = totalTimerHours + Convert.ToDateTime(stopTime).TimeOfDay.Subtract(Convert.ToDateTime(timerSet.Properties["StartTime"]).TimeOfDay);
                        //get the calendar hours between the start and the stop click.
                        var calendarTimerSec = from x in calenderHours
                                        where Convert.ToDateTime(x.Properties["Start"]).TimeOfDay >= Convert.ToDateTime(timerSet.Properties["StartTime"]).TimeOfDay
                                        && Convert.ToDateTime(x.Properties["End"]).TimeOfDay <= Convert.ToDateTime(stopTime).TimeOfDay
                                        select x;
                        //get the email hours between the start and the stop click.
                        var emailTimerSec = from x in emailHours
                                            where Convert.ToDateTime(x.Properties["DateTime"]).TimeOfDay >= Convert.ToDateTime(timerSet.Properties["StartTime"]).TimeOfDay
                                            && Convert.ToDateTime(x.Properties["DateTime"]).TimeOfDay <= Convert.ToDateTime(stopTime).TimeOfDay
                                            select x;
                        //Calculate the calendar hours.
                        foreach (var item in calendarTimerSec)
                        {
                            var startTime = Convert.ToDateTime(item.Properties["Start"]);
                            var endTime = Convert.ToDateTime(item.Properties["End"]);
                            var span = endTime.Subtract(startTime);
                            calendarTimerHours = calendarTimerHours + Convert.ToInt16(span.Hours);
                            calendarTimerMinutes = calendarTimerMinutes + Convert.ToInt16(span.Minutes);
                        }

                        //calculate the email hours.
                        int receivedEmailCount = 0;
                        int sentEmailCount = 0;
                        foreach (var item in emailTimerSec)
                        {
                            if (item.Properties["EmailType"].ToString() == "received")
                            {
                                if (Convert.ToBoolean(item.Properties["IsRead"])) receivedEmailCount = receivedEmailCount + 1;
                            }
                            else if (item.Properties["EmailType"].ToString() == "sent")
                            {
                                sentEmailCount = sentEmailCount + 1;
                            }
                        }
                        TimeSpan emailspan = TimeSpan.FromMinutes((sentEmailCount * _timeTrackerOptions.SentEmailTime) + (receivedEmailCount * _timeTrackerOptions.ReceivedEmailTime));
                        emailTimerHours = emailTimerHours + Convert.ToInt16(emailspan.Hours);
                        emailTimerMinutes = emailTimerMinutes + Convert.ToInt16(emailspan.Minutes);
                    }

                    //calculate other hours based on the total email and calendar hours
                    otherTimerHours = totalTimerHours.Hours - (emailTimerHours + calendarTimerHours);
                    if (totalTimerHours.Minutes < (emailTimerMinutes + calendarTimerMinutes))
                    {
                        if (((emailTimerMinutes + calendarTimerMinutes) - totalTimerHours.Minutes) < 60)
                        {
                            otherTimerMinutes = 60 - ((emailTimerMinutes + calendarTimerMinutes) - totalTimerHours.Minutes);
                            otherTimerHours = otherTimerHours - 1;
                        }
                        else
                        {
                            otherTimerMinutes = ((emailTimerMinutes + calendarTimerMinutes) - totalTimerHours.Minutes) - 60;
                            otherTimerHours = otherTimerHours - 2;
                        }
                    }
                    else
                    {
                        otherTimerMinutes = totalTimerHours.Minutes - (emailTimerMinutes + calendarTimerMinutes);
                    }

                    //get work hours list.
                    var workHoursList = new SiteList
                    {
                        SiteId = _timeTrackerOptions.SharePointSiteId,
                        ListId = _timeTrackerOptions.WorkHoursListPrefix + userObjectIdentifier
                    };

                    //update workhours list date entry with timer hours.
                    dynamic fieldsObject = new JObject();
                    fieldsObject.MeetingTimerHours = calendarTimerHours.ToString();
                    fieldsObject.MeetingTimerMinutes = calendarTimerMinutes.ToString();
                    fieldsObject.EmailTimerHours = emailTimerHours.ToString();
                    fieldsObject.EmailTimerMinutes = emailTimerMinutes.ToString();
                    fieldsObject.OtherTimerHours = otherTimerHours.ToString();
                    fieldsObject.OtherTimerMinutes = otherTimerMinutes.ToString();

                    dynamic timerHoursRootObject = new JObject();
                    timerHoursRootObject.fields = fieldsObject;
                    // Update List Item in WorkHours List
                    await _graphSharePointService.UpdateSiteListItemAsync(workHoursList, id, timerHoursRootObject.ToString());
                }
            }
            catch(Exception ex)
            {
                throw;
            }
        }
        private async void updateStatus(string status, string id)
        {

            dynamic workHoursFieldsObject = new JObject();

            switch(status)
            {
                case "scheduled":
                    workHoursFieldsObject.DailyUpdateStatus = "scheduled";
                    break;
                case "updated":
                    workHoursFieldsObject.DailyUpdateStatus = "scheduled";
                    workHoursFieldsObject.UpdateDate = DateTime.Now;
                    break;
                case "inprogress":
                    workHoursFieldsObject.DailyUpdateStatus = "inprogress";
                    break;
            }

            dynamic workHoursRootObject = new JObject();
            workHoursRootObject.fields = workHoursFieldsObject;
            // Update List Item in WorkHours List
            await _graphSharePointService.UpdateSiteListItemAsync(_usersSiteList, id, workHoursRootObject.ToString());
        }
    }
}
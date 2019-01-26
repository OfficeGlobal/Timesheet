// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GoLocal.TimeTracker.Dashboard.Models;
using GoLocal.TimeTracker.MiddleTier.Models;

namespace GoLocal.TimeTracker.Dashboard.ViewModels
{
    public class DashBoardViewModel
    {
        public MiddleTier.Models.UserProfile UserInfo { get; set; }

        public int TodayHours { get; set; }
        public int TodayMinutes { get; set; }
		public string TodayGoalStatus { get; set; } = "";

        public string LastWeekGoalStatus { get; set; } = "";
		
        public string ThisMonthGoalStatus { get; set; } = "";

		public string SubmittedMonths { get; set; } = "";

		public DashboardMonthHoursViewModel ThisMonth { get; set; } = new DashboardMonthHoursViewModel();
		public DashboardMonthHoursViewModel LastMonth { get; set; } = new DashboardMonthHoursViewModel();
        public string LastMonthGoalStatus { get; set; } = "";

		public DateTime WeekStartDay { get; set; }
        public DateTime WeekEndDay { get; set; }

        public DayTotalHours[] DayHours { get; set; }

		public DashBoardWeekHoursViewModel ThisWeek { get; set; } = new DashBoardWeekHoursViewModel();

		public DashBoardWeekHoursViewModel LastWeek { get; set; } = new DashBoardWeekHoursViewModel();
		public void InitilizeObjProp()
        {
            DayHours = new DayTotalHours[7];
            
            selectedDate = DateTime.Now.AddDays(-1);

        }

        public int TodayEmailHours { get; set; }
        public int TodayEmailMins { get; set; }
        public int TodayMeetingHours { get; set; }
        public int TodayMeetingMins { get; set; }

        public int TodayOtherHours { get; set; }
        public int TodayOtherMins { get; set; }

        public string ManagerName { get; set; }

        public string AdjustedHoursReason { get; set; }
        public DateTime SubmittedOn { get; set; }

        public int YdayGoal { get; set; } = 0;
        public int LwGoal { get; set; } = 0;
        public int TmGoal { get; set; } = 0;
        public int LmGoal { get; set; } = 0;

        public int YdayGoalMin { get; set; } = 0;
        public int LwGoalMin { get; set; } = 0;
        public int TmGoalMin { get; set; } = 0;
        public int LmGoalMin { get; set; } = 0;

        public int TodayOTHr { get; set; }
        public int TodayOTMin { get; set; }
     
        public DateTime selectedDate { get; set; }

        public bool WorkHoursEditable { get; set; }

		public bool currentWeekData { get; set; }

        public bool EnableTimer { get; set; }

        // Get Overtime hr/min for all
        public void OTcomputation(Int16 DailyGoal,Int16 WeeklyGoal,Int16 MonthlyGoal)
        {
            Int16 DailyGoalMin = 0;
            Int16 WeeklyGoalMin = 0;
            Int16 MonthlyGoalMin = 0;

            YdayGoal = DailyGoal;
            LwGoal = WeeklyGoal;
            TmGoal = MonthlyGoal;
            LmGoal = MonthlyGoal;
         
			if ((DailyGoal > 0) && (WeeklyGoal == 0) && (MonthlyGoal == 0))
            {  
                if ((TodayHours > DailyGoal) || ((TodayHours == DailyGoal) && (TodayMinutes > DailyGoalMin)))
                {
                    TodayOTHr = TodayHours - DailyGoal;
                    TodayOTMin = TodayMinutes - DailyGoalMin;
                }
            }

            if ((WeeklyGoal > 0) && (MonthlyGoal == 0))
            {
                // Calculate Weekly if Hours Exceed Goal.
                int tempWeeklyMins = (ThisWeek.EmailMinutes + ThisWeek.MeetingMinutes + ThisWeek.OthWorkMinutes);
                int tempWeeklyhours = ((ThisWeek.EmailHours + ThisWeek.MeetingHours + ThisWeek.OthWorkHours) + (tempWeeklyMins / 60));

                if ((tempWeeklyhours > WeeklyGoal) || ((tempWeeklyhours == WeeklyGoal) && ((tempWeeklyMins % 60) > WeeklyGoalMin)))
                {
					ThisWeek.OTHours = tempWeeklyhours - WeeklyGoal;
                    ThisWeek.OTMinutes = (tempWeeklyMins % 60) - WeeklyGoalMin;
                }           
                if ((LastWeek.TotalHours > WeeklyGoal) || 
                    ((LastWeek.TotalHours == WeeklyGoal) && (LastWeek.TotalMins > WeeklyGoalMin)))
				{
					LastWeek.OTHours = LastWeek.TotalHours - WeeklyGoal;
					LastWeek.OTMinutes = LastWeek.TotalMins - WeeklyGoalMin;
				}
			}

            if (MonthlyGoal > 0)
            {
				// Calculate Monthly if Hours Exceed Goal.
				if ((ThisMonth.TotalHours> MonthlyGoal) || 
                    ((ThisMonth.TotalHours == MonthlyGoal) && (ThisMonth.TotalMins > MonthlyGoalMin)))
				{
					ThisMonth.OverTimeHours = ThisMonth.TotalHours - MonthlyGoal;
					ThisMonth.OverTimeMins = ThisMonth.TotalMins - MonthlyGoalMin;
				}

                
                if ((LastMonth.TotalHours > MonthlyGoal) ||
                    ((LastMonth.TotalHours == MonthlyGoal) && (LastMonth.TotalMins > MonthlyGoalMin)))
                {
					LastMonth.OverTimeHours = LastMonth.TotalHours - MonthlyGoal;
					LastMonth.OverTimeMins = LastMonth.TotalMins - MonthlyGoalMin;
				}
			}

            TodayGoalStatus = (((TodayHours > DailyGoal) && TodayOTHr>0) || (TodayHours == DailyGoal && TodayMinutes > DailyGoalMin) && TodayOTMin > 0) ? "Overtime" : "RegularHrs";
			LastWeekGoalStatus = (((LastWeek.TotalHours > WeeklyGoal) && LastWeek.OTHours > 0) || (LastWeek.TotalHours == WeeklyGoal && LastWeek.TotalMins > WeeklyGoalMin) && LastWeek.OTMinutes > 0) ? "Overtime" : "RegularHrs";
			ThisMonthGoalStatus = (((ThisMonth.TotalHours > MonthlyGoal) && ThisMonth.OverTimeHours > 0) || (ThisMonth.TotalHours == MonthlyGoal && ThisMonth.TotalMins > MonthlyGoalMin) && ThisMonth.OverTimeHours > 0) ? "Overtime" : "RegularHrs";

			LastMonthGoalStatus = (((LastMonth.TotalHours > MonthlyGoal) && LastMonth.OverTimeHours > 0) || (LastMonth.TotalHours == MonthlyGoal && LastMonth.TotalMins > MonthlyGoalMin) && LastMonth.OverTimeMins > 0) ? "Overtime" : "RegularHrs";

		}
    }
}

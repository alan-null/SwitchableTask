using System;
using System.Collections;
using Sitecore;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.Tasks;

namespace SwitchableTask
{
    class CustomDatabaseAgent : DatabaseAgent
    {
        private readonly string _databaseName;

        public CustomDatabaseAgent(string databaseName, string scheduleRoot)
            : base(databaseName, scheduleRoot)
        {
            _databaseName = databaseName;
        }

        public new void Run()
        {
            LogInfo("Scheduling.DatabaseAgent started. Database: " + _databaseName);
            Job job = Context.Job;
            ScheduleItem[] schedules = GetSchedules();
            LogInfo("Examining schedules (count: " + schedules.Length + ")");
            if (IsValidJob(job))
            {
                job.Status.Total = schedules.Length;
            }
            foreach (ScheduleItem scheduleItem in schedules)
            {
                try
                {
                    if (scheduleItem.IsDue)
                    {
                        LogInfo("Starting: " + scheduleItem.Name + (scheduleItem.Asynchronous ? " (asynchronously)" : string.Empty));
                        scheduleItem.Execute();
                        LogInfo("Ended: " + scheduleItem.Name);
                    }
                    else
                        LogInfo("Not due: " + scheduleItem.Name);
                    if (scheduleItem.AutoRemove)
                    {
                        if (scheduleItem.Expired)
                        {
                            LogInfo("Schedule is expired. Auto removing schedule item: " + scheduleItem.Name);
                            scheduleItem.Remove();
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Something went wrong", e, this);
                }
                if (IsValidJob(job))
                {
                    ++job.Status.Processed;
                }
            }
        }

        private bool IsValidJob(Job job)
        {
            return job != null && job.Category == "schedule";
        }

        private void LogInfo(string message)
        {
            if (LogActivity)
            {
                Log.Info(message, this);
            }
        }

        private ScheduleItem[] GetSchedules()
        {
            Item obj = Database.Items[ScheduleRoot];
            if (obj == null)
            {
                return new ScheduleItem[0];
            }
            ArrayList arrayList = new ArrayList();
            foreach (Item innerItem in obj.Axes.GetDescendants())
            {
                if (innerItem.TemplateID == TemplateIDs.Schedule)
                {
                    arrayList.Add(new ScheduleItem(innerItem));
                }
            }
            return arrayList.ToArray(typeof(ScheduleItem)) as ScheduleItem[];
        }
    }
}

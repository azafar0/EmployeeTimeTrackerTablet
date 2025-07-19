using System;

namespace EmployeeTimeTracker.Models
{
    /// <summary>
    /// Model class for Summary View reporting
    /// Used by DateRangeReportForm for Summary View display showing aggregated time data
    /// </summary>
    public class TimeReportSummary
    {
        /// <summary>
        /// The key used for grouping (e.g., employee name, job title, week, etc.)
        /// </summary>
        public string GroupKey { get; set; } = "";

        /// <summary>
        /// The type of grouping applied (Employee, JobTitle, Week, Month, Date)
        /// </summary>
        public string GroupType { get; set; } = "";

        /// <summary>
        /// Employee name (used when grouping by employee)
        /// </summary>
        public string EmployeeName { get; set; } = "";

        /// <summary>
        /// Job title (used when grouping by employee or for display)
        /// </summary>
        public string JobTitle { get; set; } = "";

        /// <summary>
        /// Total hours worked for this group
        /// </summary>
        public decimal TotalHours { get; set; }

        /// <summary>
        /// Total gross pay for this group
        /// </summary>
        public decimal TotalPay { get; set; }

        /// <summary>
        /// Number of unique days worked in this group
        /// </summary>
        public int DaysWorked { get; set; }

        /// <summary>
        /// Average hours per day worked
        /// </summary>
        public decimal AverageHoursPerDay { get; set; }

        /// <summary>
        /// Start date of the reporting period
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date of the reporting period
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Gets formatted total hours string for display
        /// </summary>
        public string TotalHoursFormatted => $"{TotalHours:F1}";

        /// <summary>
        /// Gets formatted total pay string for display
        /// </summary>
        public string TotalPayFormatted => $"${TotalPay:F2}";

        /// <summary>
        /// Gets formatted average hours per day string for display
        /// </summary>
        public string AverageHoursPerDayFormatted => $"{AverageHoursPerDay:F1}";

        /// <summary>
        /// Gets a summary description for this grouping
        /// </summary>
        public string SummaryDescription
        {
            get
            {
                return GroupType.ToLower() switch
                {
                    "employee" => $"{EmployeeName} - {JobTitle}",
                    "jobtitle" => $"Job Title: {GroupKey}",
                    "week" => $"Week: {GroupKey}",
                    "month" => $"Month: {GroupKey}",
                    "date" => $"Date: {GroupKey}",
                    _ => GroupKey
                };
            }
        }
    }
}
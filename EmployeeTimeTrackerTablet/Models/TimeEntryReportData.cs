using System;

namespace EmployeeTimeTracker.Models
{
    /// <summary>
    /// Model class for Detail View reporting
    /// Used by DateRangeReportForm for Detail View display showing individual time entries
    /// Enhanced with photo path support for tablet time tracking
    /// </summary>
    public class TimeEntryReportData
    {
        /// <summary>
        /// Original TimeEntry ID for reference
        /// </summary>
        public int EntryID { get; set; }

        /// <summary>
        /// Employee ID for internal reference
        /// </summary>
        public int EmployeeID { get; set; }

        /// <summary>
        /// Employee's full name for display
        /// </summary>
        public string EmployeeName { get; set; } = "";

        /// <summary>
        /// Employee's job title for display and filtering
        /// </summary>
        public string JobTitle { get; set; } = "";

        /// <summary>
        /// Date of the work shift
        /// </summary>
        public DateTime ShiftDate { get; set; }

        /// <summary>
        /// Day of the week name (Monday, Tuesday, etc.)
        /// </summary>
        public string DayName { get; set; } = "";

        /// <summary>
        /// Time the employee clocked in (optional for incomplete entries)
        /// </summary>
        public TimeSpan? TimeIn { get; set; }

        /// <summary>
        /// Time the employee clocked out (optional for incomplete entries)
        /// </summary>
        public TimeSpan? TimeOut { get; set; }

        /// <summary>
        /// Total hours worked for this shift
        /// </summary>
        public decimal TotalHours { get; set; }

        /// <summary>
        /// Gross pay for this shift (TotalHours * PayRate)
        /// </summary>
        public decimal GrossPay { get; set; }

        /// <summary>
        /// Notes or comments for this time entry
        /// </summary>
        public string Notes { get; set; } = "";

        /// <summary>
        /// Date the time entry was created
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Date the time entry was last modified
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// Path to the photo taken at clock-in (tablet feature)
        /// </summary>
        public string? ClockInPhotoPath { get; set; }

        /// <summary>
        /// Path to the photo taken at clock-out (tablet feature)
        /// </summary>
        public string? ClockOutPhotoPath { get; set; }

        /// <summary>
        /// Indicates if this is an incomplete entry (missing clock in/out)
        /// </summary>
        public bool IsIncomplete => !TimeIn.HasValue || !TimeOut.HasValue;

        /// <summary>
        /// Gets formatted time in string for display (12-hour format)
        /// </summary>
        public string TimeInFormatted
        {
            get
            {
                if (!TimeIn.HasValue) return "";
                try
                {
                    var dateTime = DateTime.Today.Add(TimeIn.Value);
                    return dateTime.ToString("h:mm tt");
                }
                catch
                {
                    return TimeIn.Value.ToString(@"hh\:mm");
                }
            }
        }

        /// <summary>
        /// Gets formatted time out string for display (12-hour format)
        /// </summary>
        public string TimeOutFormatted
        {
            get
            {
                if (!TimeOut.HasValue) return "";
                try
                {
                    var dateTime = DateTime.Today.Add(TimeOut.Value);
                    return dateTime.ToString("h:mm tt");
                }
                catch
                {
                    return TimeOut.Value.ToString(@"hh\:mm");
                }
            }
        }

        /// <summary>
        /// Gets a status indicator for the time entry
        /// </summary>
        public string Status
        {
            get
            {
                if (IsIncomplete) return "Incomplete";
                if (TotalHours == 0) return "No Hours";
                if (TotalHours >= 8) return "Full Day";
                return "Partial Day";
            }
        }
    }
}
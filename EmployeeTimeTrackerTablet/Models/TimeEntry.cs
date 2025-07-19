using System;

namespace EmployeeTimeTracker.Models
{
    public class TimeEntry
    {
        public int EntryID { get; set; }
        public int EmployeeID { get; set; }
        public DateTime ShiftDate { get; set; }
        public TimeSpan? TimeIn { get; set; }
        public TimeSpan? TimeOut { get; set; }
        
        /// <summary>
        /// Gets or sets the actual date and time when the employee clocked in.
        /// Used for cross-midnight shift support and accurate timestamp tracking.
        /// </summary>
        public DateTime? ActualClockInDateTime { get; set; }
        
        /// <summary>
        /// Gets or sets the actual date and time when the employee clocked out.
        /// Used for cross-midnight shift support and accurate timestamp tracking.
        /// </summary>
        public DateTime? ActualClockOutDateTime { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether this time entry is currently active.
        /// True when employee is clocked in, false when clocked out or shift is completed.
        /// </summary>
        public bool IsActive { get; set; } = false;
        
        public decimal TotalHours { get; set; }
        public decimal GrossPay { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        // Photo capture support
        public string? ClockInPhotoPath { get; set; }
        public string? ClockOutPhotoPath { get; set; }
    }
}
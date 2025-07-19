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
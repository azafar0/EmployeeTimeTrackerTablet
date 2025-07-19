using System;

namespace EmployeeTimeTracker.Models
{
    /// <summary>
    /// Represents the current shift status of an employee, including cross-midnight shift support.
    /// Used by ViewModels to determine employee availability and working status.
    /// </summary>
    public class EmployeeShiftStatus
    {
        /// <summary>
        /// Gets or sets a value indicating whether the employee is currently working (clocked in).
        /// </summary>
        public bool IsWorking { get; set; }

        /// <summary>
        /// Gets or sets the date when the current shift started.
        /// For cross-midnight shifts, this is the date when the shift began.
        /// </summary>
        public DateTime CurrentShiftDate { get; set; }

        /// <summary>
        /// Gets or sets the total hours worked in the current shift.
        /// For ongoing shifts, this is calculated from ActualClockInDateTime to now.
        /// </summary>
        public double WorkingHours { get; set; }

        /// <summary>
        /// Gets or sets the actual date and time when the shift started.
        /// </summary>
        public DateTime ShiftStarted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this shift crosses midnight.
        /// True when the shift started on a different calendar day than today.
        /// </summary>
        public bool IsCrossMidnight { get; set; }

        /// <summary>
        /// Gets or sets the total hours worked today (completed shifts only).
        /// Does not include hours from ongoing shifts.
        /// </summary>
        public decimal TodayCompletedHours { get; set; }

        /// <summary>
        /// Gets or sets the date and time of the last clock out for this employee.
        /// Null if the employee has never clocked out or is currently working.
        /// </summary>
        public DateTime? LastClockOut { get; set; }
    }
}
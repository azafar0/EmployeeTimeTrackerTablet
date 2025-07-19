using EmployeeTimeTracker.Models;

namespace EmployeeTimeTrackerTablet.Models
{
    /// <summary>
    /// Represents the result of a time tracking validation operation.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the validation passed.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the error message if validation failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        /// <returns>A ValidationResult indicating success.</returns>
        public static ValidationResult Success() => new() { IsValid = true };

        /// <summary>
        /// Creates a failed validation result with an error message.
        /// </summary>
        /// <param name="errorMessage">The error message describing the validation failure.</param>
        /// <returns>A ValidationResult indicating failure.</returns>
        public static ValidationResult Failure(string errorMessage) => new() { IsValid = false, ErrorMessage = errorMessage };
    }

    /// <summary>
    /// Represents the result of a time tracking operation (clock in/out).
    /// </summary>
    public class TimeTrackingResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the operation succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if the operation failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the time entry that was created or updated.
        /// </summary>
        public TimeEntry? TimeEntry { get; set; }

        /// <summary>
        /// Creates a successful time tracking result.
        /// </summary>
        /// <param name="timeEntry">The time entry that was created or updated.</param>
        /// <returns>A TimeTrackingResult indicating success.</returns>
        public static TimeTrackingResult CreateSuccess(TimeEntry timeEntry) => new() { Success = true, TimeEntry = timeEntry };

        /// <summary>
        /// Creates a failed time tracking result with an error message.
        /// </summary>
        /// <param name="errorMessage">The error message describing the failure.</param>
        /// <returns>A TimeTrackingResult indicating failure.</returns>
        public static TimeTrackingResult CreateFailure(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
    }
}
using System;
using System.Threading.Tasks;
using EmployeeTimeTracker.Models;
using EmployeeTimeTracker.Data;
using EmployeeTimeTracker.Utilities;
using EmployeeTimeTrackerTablet.Models;

namespace EmployeeTimeTrackerTablet.Services
{
    /// <summary>
    /// Provides comprehensive business logic for tablet-specific time tracking operations,
    /// including clock-in/out, validations, and status checks.
    /// </summary>
    public class TabletTimeService
    {
        private readonly TimeEntryRepository _timeEntryRepository;
        private readonly EmployeeRepository _employeeRepository;
        private readonly PhotoCaptureService _photoCaptureService;

        #region Business Rule Constants
        
        /// <summary>
        /// Maximum allowed shift duration (16 hours) to prevent employee overwork and ensure compliance with labor laws.
        /// </summary>
        private static readonly TimeSpan MAX_SHIFT_DURATION = TimeSpan.FromHours(16);
        
        /// <summary>
        /// Minimum cooldown period (4 hours) required between consecutive shifts to ensure adequate rest time.
        /// </summary>
        private static readonly TimeSpan CLOCK_IN_COOLDOWN = TimeSpan.FromHours(4);
        
        #endregion

        /// <summary>
        /// Initializes a new instance of the TabletTimeService.
        /// </summary>
        /// <param name="timeEntryRepository">The repository for managing time entries.</param>
        /// <param name="employeeRepository">The repository for managing employee data.</param>
        /// <param name="photoCaptureService">The service for capturing photos during time tracking operations.</param>
        public TabletTimeService(
            TimeEntryRepository timeEntryRepository,
            EmployeeRepository employeeRepository,
            PhotoCaptureService photoCaptureService)
        {
            _timeEntryRepository = timeEntryRepository ?? throw new ArgumentNullException(nameof(timeEntryRepository));
            _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
            _photoCaptureService = photoCaptureService ?? throw new ArgumentNullException(nameof(photoCaptureService));
        }

        #region Core Methods

        /// <summary>
        /// Performs the clock-in operation for an employee.
        /// Includes validation, creation of a new time entry, and photo capture.
        /// FIXED: Now properly saves photo path to TimeEntry record.
        /// </summary>
        /// <param name="employeeId">The ID of the employee clocking in.</param>
        /// <param name="notes">Optional notes for the time entry.</param>
        /// <returns>A TimeTrackingResult indicating success or failure with details.</returns>
        public async Task<TimeTrackingResult> ClockInAsync(int employeeId, string notes = "")
        {
            try
            {
                // Step 1: Validate the clock-in request
                var validation = await ValidateClockInAsync(employeeId);
                if (!validation.IsValid)
                {
                    return TimeTrackingResult.CreateFailure(validation.ErrorMessage);
                }

                // Step 2: Retrieve employee details for additional data (e.g., hourly rate, audit trail)
                var employee = await Task.Run(() => _employeeRepository.GetEmployeeById(employeeId));
                if (employee == null) // Should not happen if ValidateClockInAsync passed, but good defensive coding
                {
                    return TimeTrackingResult.CreateFailure("Employee not found during clock-in process.");
                }

                // Step 3: Capture photo
                string? clockInPhotoPath = await CaptureClockInPhotoAsync(employeeId);
                System.Diagnostics.Debug.WriteLine($"TabletTimeService: Clock-in photo path: {clockInPhotoPath ?? "NULL"}");

                // Step 4: Create a new time entry object WITH PHOTO PATH
                var timeEntry = new TimeEntry
                {
                    EmployeeID = employeeId,
                    ShiftDate = DateTime.Today,
                    TimeIn = DateTime.Now.TimeOfDay,
                    TimeOut = null, // Will be set during clock out
                    ClockInPhotoPath = clockInPhotoPath, // FIXED: Save the photo path
                    Notes = notes?.Trim() ?? string.Empty,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };

                System.Diagnostics.Debug.WriteLine($"TabletTimeService: TimeEntry created with ClockInPhotoPath: {timeEntry.ClockInPhotoPath ?? "NULL"}");

                // Step 5: Save the new time entry to the repository
                var success = await _timeEntryRepository.AddAsync(timeEntry);

                if (success)
                {
                    // Log: Successful clock-in.
                    System.Diagnostics.Debug.WriteLine($"TabletTimeService: Clock in successful for {employee.FirstName} {employee.LastName} with photo: {clockInPhotoPath ?? "simulation"}");
                    return TimeTrackingResult.CreateSuccess(timeEntry);
                }
                else
                {
                    return TimeTrackingResult.CreateFailure("Failed to save clock-in record. Please try again.");
                }
            }
            catch (Exception ex)
            {
                // Log the exception for debugging purposes.
                System.Diagnostics.Debug.WriteLine($"TabletTimeService Error during clock in for employee {employeeId}: {ex.Message}");
                return TimeTrackingResult.CreateFailure("System error during clock in. Please try again.");
            }
        }

        /// <summary>
        /// Performs the clock-out operation for an employee.
        /// Includes validation, updating an existing time entry, calculating hours/pay,
        /// and photo capture.
        /// FIXED: Now properly saves photo path to TimeEntry record.
        /// </summary>
        /// <param name="employeeId">The ID of the employee clocking out.</param>
        /// <param name="notes">Optional notes to append to the time entry.</param>
        /// <returns>A TimeTrackingResult indicating success or failure with details.</returns>
        public async Task<TimeTrackingResult> ClockOutAsync(int employeeId, string notes = "")
        {
            try
            {
                // Step 1: Validate the clock-out request
                var validation = await ValidateClockOutAsync(employeeId);
                if (!validation.IsValid)
                {
                    return TimeTrackingResult.CreateFailure(validation.ErrorMessage);
                }

                // Step 2: Get the current open time entry
                var timeEntry = await _timeEntryRepository.GetCurrentTimeEntryAsync(employeeId);
                if (timeEntry == null) // Should not happen if ValidateClockOutAsync passed
                {
                    return TimeTrackingResult.CreateFailure("No open time entry found for clock out.");
                }

                // Step 3: Get employee details for hourly rate calculation
                var employee = await Task.Run(() => _employeeRepository.GetEmployeeById(employeeId));
                if (employee == null) // Should not happen if validation passed, but defensive.
                {
                    return TimeTrackingResult.CreateFailure("Employee details not found during clock-out calculation.");
                }

                // Step 4: Capture photo
                string? clockOutPhotoPath = await CaptureClockOutPhotoAsync(employeeId);
                System.Diagnostics.Debug.WriteLine($"TabletTimeService: Clock-out photo path: {clockOutPhotoPath ?? "NULL"}");

                // Step 5: Update time entry with clock-out time, photo path, and notes
                timeEntry.TimeOut = DateTime.Now.TimeOfDay;
                timeEntry.ClockOutPhotoPath = clockOutPhotoPath; // FIXED: Save the photo path
                if (!string.IsNullOrWhiteSpace(notes))
                {
                    timeEntry.Notes = string.IsNullOrWhiteSpace(timeEntry.Notes)
                        ? notes.Trim()
                        : $"{timeEntry.Notes}; {notes.Trim()}"; // Append new notes
                }

                System.Diagnostics.Debug.WriteLine($"TabletTimeService: TimeEntry updated with ClockOutPhotoPath: {timeEntry.ClockOutPhotoPath ?? "NULL"}");

                // Step 6: Calculate total hours and gross pay (Business Rules: Deduct lunch if > 6 hours)
                if (timeEntry.TimeIn.HasValue && timeEntry.TimeOut.HasValue)
                {
                    var timeCalculation = CalculateHoursAndPay(timeEntry);
                    timeEntry.TotalHours = (decimal)timeCalculation.totalHours;
                    timeEntry.GrossPay = timeEntry.TotalHours * employee.PayRate;
                }

                // Step 7: Update the time entry in the repository
                timeEntry.ModifiedDate = DateTime.Now;
                var success = await _timeEntryRepository.UpdateAsync(timeEntry);

                if (success)
                {
                    // Log: Successful clock-out.
                    System.Diagnostics.Debug.WriteLine($"TabletTimeService: Clock out successful for {employee.FirstName} {employee.LastName} with photo: {clockOutPhotoPath ?? "simulation"}");
                    return TimeTrackingResult.CreateSuccess(timeEntry);
                }
                else
                {
                    return TimeTrackingResult.CreateFailure("Failed to save clock-out record. Please try again.");
                }
            }
            catch (Exception ex)
            {
                // Log the exception for debugging purposes.
                System.Diagnostics.Debug.WriteLine($"TabletTimeService Error during clock out for employee {employeeId}: {ex.Message}");
                return TimeTrackingResult.CreateFailure("System error during clock out. Please try again.");
            }
        }

        /// <summary>
        /// Gets the current status string for an employee (simple string version).
        /// </summary>
        /// <param name="employeeId">The ID of the employee to check.</param>
        /// <returns>A status string like "Clocked In since 9:00 AM" or "Available".</returns>
        public async Task<string> GetEmployeeStatusAsync(int employeeId)
        {
            try
            {
                var employee = await Task.Run(() => _employeeRepository.GetEmployeeById(employeeId));
                if (employee == null)
                {
                    return "Employee not found";
                }

                var currentEntry = await _timeEntryRepository.GetCurrentTimeEntryAsync(employeeId);

                if (currentEntry != null && currentEntry.TimeIn.HasValue && !currentEntry.TimeOut.HasValue)
                {
                    var clockInTime = DateTime.Today.Add(currentEntry.TimeIn.Value);
                    return $"Clocked In since {clockInTime:h:mm tt}";
                }
                else
                {
                    return "Available";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TabletTimeService Error getting employee status for {employeeId}: {ex.Message}");
                return "Status unavailable";
            }
        }

        /// <summary>
        /// Gets today's total hours worked for an employee.
        /// </summary>
        /// <param name="employeeId">The ID of the employee.</param>
        /// <returns>TimeSpan representing total hours worked today.</returns>
        public async Task<TimeSpan> GetTodaysHoursAsync(int employeeId)
        {
            try
            {
                var todayEntries = await _timeEntryRepository.GetTimeEntriesForDateAsync(employeeId, DateTime.Today);
                var totalHours = 0.0;

                foreach (var entry in todayEntries)
                {
                    if (entry.TimeIn.HasValue)
                    {
                        if (entry.TimeOut.HasValue)
                        {
                            // Completed entry - use calculated total hours
                            totalHours += (double)entry.TotalHours;
                        }
                        else
                        {
                            // Currently clocked in - calculate current working time
                            var clockInTime = DateTime.Today.Add(entry.TimeIn.Value);
                            var currentWorkTime = DateTime.Now - clockInTime;
                            totalHours += currentWorkTime.TotalHours;
                        }
                    }
                }

                return TimeSpan.FromHours(totalHours);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TabletTimeService Error getting today's hours for employee {employeeId}: {ex.Message}");
                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Retrieves the current clock status of an employee.
        /// </summary>
        /// <param name="employeeId">The ID of the employee to check.</param>
        /// <returns>An EmployeeStatusResult indicating success, status, clock-in time, and work duration.</returns>
        public async Task<EmployeeStatusResult> GetCurrentStatusAsync(int employeeId)
        {
            try
            {
                // Verify employee exists (redundant if validated elsewhere, but safe)
                var employee = await Task.Run(() => _employeeRepository.GetEmployeeById(employeeId));
                if (employee == null)
                {
                    return new EmployeeStatusResult(false, "Employee not found.");
                }

                var currentEntry = await _timeEntryRepository.GetCurrentTimeEntryAsync(employeeId);

                if (currentEntry != null && currentEntry.TimeIn.HasValue && !currentEntry.TimeOut.HasValue)
                {
                    // Employee is clocked in
                    TimeSpan? workTime = null;
                    DateTime? clockInDateTime = null;
                    
                    if (currentEntry.TimeIn.HasValue)
                    {
                        clockInDateTime = DateTime.Today.Add(currentEntry.TimeIn.Value);
                        workTime = DateTime.Now - clockInDateTime;
                    }
                    
                    return new EmployeeStatusResult(true, "Clocked In", clockInDateTime, workTime);
                }
                else
                {
                    // Employee is not clocked in
                    return new EmployeeStatusResult(true, "Available", null, null);
                }
            }
            catch (Exception ex)
            {
                // Log the exception.
                System.Diagnostics.Debug.WriteLine($"TabletTimeService Error getting current status for employee {employeeId}: {ex.Message}");
                return new EmployeeStatusResult(false, "System error checking status.");
            }
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Validates if an employee can clock in. Checks for existence, activity status,
        /// and ensures no duplicate clock-ins for the same day or open entries.
        /// ENHANCED: Added 4-hour cooldown period validation between shifts.
        /// </summary>
        /// <param name="employeeId">The ID of the employee.</param>
        /// <returns>A ValidationResult indicating success or failure with a message.</returns>
        public async Task<ValidationResult> ValidateClockInAsync(int employeeId)
        {
            try
            {
                // Validate Employee Existence and Activity
                var employee = await Task.Run(() => _employeeRepository.GetEmployeeById(employeeId));
                if (employee == null)
                {
                    return ValidationResult.Failure("Employee not found. Please try another ID.");
                }
                if (!employee.Active)
                {
                    return ValidationResult.Failure($"{employee.FirstName} {employee.LastName} is not currently active.");
                }

                // Check for existing open time entry (Business Rule: No double clock-ins)
                var currentEntry = await _timeEntryRepository.GetCurrentTimeEntryAsync(employeeId);
                if (currentEntry != null && currentEntry.TimeIn.HasValue && !currentEntry.TimeOut.HasValue)
                {
                    var clockInTime = currentEntry.TimeIn?.ToString("h\\:mm") ?? "earlier today";
                    return ValidationResult.Failure($"{employee.FirstName} {employee.LastName} is already clocked in since {clockInTime}.");
                }

                // REMOVED: Same-day completed work restriction to allow multiple shifts with cooldown
                // Previously: if (todayEntries.Count > 0 && todayEntries.Exists(e => e.TimeOut.HasValue && e.TimeIn.HasValue))
                // This blocked employees from working multiple shifts in the same day
                // Now replaced with 4-hour cooldown validation below

                // NEW BUSINESS RULE: Enforce 4-hour cooldown period between shifts
                var lastCompletedEntry = await _timeEntryRepository.GetMostRecentCompletedTimeEntryAsync(employeeId);
                if (lastCompletedEntry != null && lastCompletedEntry.TimeOut.HasValue)
                {
                    // Calculate the actual clock-out time from the last completed shift
                    var lastClockOutDateTime = lastCompletedEntry.ShiftDate.Add(lastCompletedEntry.TimeOut.Value);
                    var timeSinceLastClockOut = DateTime.Now - lastClockOutDateTime;
                    
                    if (timeSinceLastClockOut < CLOCK_IN_COOLDOWN)
                    {
                        var remainingCooldown = CLOCK_IN_COOLDOWN - timeSinceLastClockOut;
                        var availableAt = DateTime.Now.Add(remainingCooldown);
                        
                        System.Diagnostics.Debug.WriteLine($"TabletTimeService: Clock-in cooldown violation for {employee.FirstName} {employee.LastName}. " +
                                                          $"Last clock-out: {lastClockOutDateTime:yyyy-MM-dd HH:mm}, " +
                                                          $"Time since: {timeSinceLastClockOut:h\\:mm}, " +
                                                          $"Required: {CLOCK_IN_COOLDOWN:h\\:mm}");
                        
                        return ValidationResult.Failure($"{employee.FirstName} {employee.LastName} must wait {remainingCooldown.Hours} hours and {remainingCooldown.Minutes} minutes before clocking in again. " +
                                                       $"Available at {availableAt:h:mm tt}.");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"TabletTimeService: Cooldown period satisfied for {employee.FirstName} {employee.LastName}. " +
                                                          $"Last clock-out: {lastClockOutDateTime:yyyy-MM-dd HH:mm}, " +
                                                          $"Time since: {timeSinceLastClockOut:h\\:mm}h - ALLOWING CLOCK-IN");
                    }
                }

                // REMOVED: Business hours validation - 24-hour operation enabled
                // Previously restricted clock-in to specific hours, now allows any time
                System.Diagnostics.Debug.WriteLine("TabletTimeService: 24-hour operation enabled - no time restrictions for clock-in");

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TabletTimeService Error validating clock in for employee {employeeId}: {ex.Message}");
                return ValidationResult.Failure("System error during clock in validation. Please try again.");
            }
        }

        /// <summary>
        /// Validates if an employee can clock out. Ensures the employee exists, is active,
        /// and currently has an open time entry.
        /// ENHANCED: Added 16-hour maximum shift duration validation.
        /// </summary>
        /// <param name="employeeId">The ID of the employee.</param>
        /// <returns>A ValidationResult indicating success or failure with a message.</returns>
        public async Task<ValidationResult> ValidateClockOutAsync(int employeeId)
        {
            try
            {
                // Validate Employee Existence and Activity
                var employee = await Task.Run(() => _employeeRepository.GetEmployeeById(employeeId));
                if (employee == null)
                {
                    return ValidationResult.Failure("Employee not found. Cannot clock out.");
                }

                // Check for an existing open time entry (Business Rule: Clock out requires existing open time entry)
                var currentEntry = await _timeEntryRepository.GetCurrentTimeEntryAsync(employeeId);
                if (currentEntry == null || !currentEntry.TimeIn.HasValue || currentEntry.TimeOut.HasValue)
                {
                    return ValidationResult.Failure($"{employee.FirstName} {employee.LastName} is not currently clocked in.");
                }

                // Validate minimum work time (prevent accidental immediate clock out)
                if (currentEntry.TimeIn.HasValue)
                {
                    var today = DateTime.Today;
                    var clockInDateTime = today.Add(currentEntry.TimeIn.Value);
                    var workTime = DateTime.Now - clockInDateTime;
                    
                    if (workTime.TotalMinutes < 1) // Minimum 1 minute work time
                    {
                        return ValidationResult.Failure("Cannot clock out within 1 minute of clocking in. Contact manager if this is an error.");
                    }

                    // NEW BUSINESS RULE: Enforce 16-hour maximum shift duration
                    if (workTime > MAX_SHIFT_DURATION)
                    {
                        var shiftHours = (int)workTime.TotalHours;
                        var shiftMinutes = workTime.Minutes;
                        
                        System.Diagnostics.Debug.WriteLine($"TabletTimeService: Maximum shift duration exceeded for {employee.FirstName} {employee.LastName}. " +
                                                          $"Current shift: {shiftHours}h {shiftMinutes}m, " +
                                                          $"Maximum allowed: {MAX_SHIFT_DURATION.TotalHours}h");
                        
                        return ValidationResult.Failure($"{employee.FirstName} {employee.LastName} has exceeded the maximum shift duration of {MAX_SHIFT_DURATION.TotalHours} hours. " +
                                                       $"Current shift: {shiftHours} hours {shiftMinutes} minutes. " +
                                                       $"Please contact a manager to authorize this extended shift.");
                    }

                    // Enhanced warning for long shifts (over 12 hours but under 16 hours)
                    if (workTime.TotalHours > 12 && workTime.TotalHours <= 16)
                    {
                        var hours = (int)workTime.TotalHours;
                        var minutes = workTime.Minutes;
                        System.Diagnostics.Debug.WriteLine($"TabletTimeService: Extended shift warning for {employee.FirstName} {employee.LastName}: {hours}h {minutes}m");
                        // This is just a warning - allow the clock-out to proceed
                    }
                }

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TabletTimeService Error validating clock out for employee {employeeId}: {ex.Message}");
                return ValidationResult.Failure("System error during clock out validation. Please try again.");
            }
        }

        /// <summary>
        /// Centralizes business rules for clock-in/out eligibility.
        /// UPDATED: Removed business hours restrictions for 24-hour operation.
        /// </summary>
        /// <param name="employeeId">The ID of the employee.</param>
        /// <returns>A ValidationResult indicating success or failure with detailed message.</returns>
        private async Task<ValidationResult> ValidateClockOperationAsync(int employeeId)
        {
            try
            {
                // Check if the employee exists using _employeeRepository
                var employee = await Task.Run(() => _employeeRepository.GetEmployeeById(employeeId));
                if (employee == null)
                {
                    return ValidationResult.Failure("Employee not found. Please verify the employee ID.");
                }

                if (!employee.Active)
                {
                    return ValidationResult.Failure($"{employee.FirstName} {employee.LastName} is not currently active and cannot clock in/out.");
                }

                // Determine the employee's current clock-in/out status using _timeEntryRepository
                var currentEntry = await _timeEntryRepository.GetCurrentTimeEntryAsync(employeeId);
                bool isCurrentlyClockedIn = currentEntry != null && currentEntry.TimeIn.HasValue && !currentEntry.TimeOut.HasValue;

                // REMOVED: Business hours validation - 24-hour operation enabled
                // Previously restricted operations to specific hours, now allows any time
                System.Diagnostics.Debug.WriteLine("TabletTimeService: 24-hour operation enabled - no time restrictions for clock operations");

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TabletTimeService Error in ValidateClockOperationAsync for employee {employeeId}: {ex.Message}");
                return ValidationResult.Failure("System error during clock operation validation. Please try again.");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Calculates total hours worked for a TimeEntry, including lunch break logic.
        /// This method was mentioned in the original prompt.
        /// </summary>
        /// <param name="entry">The TimeEntry to calculate hours for.</param>
        /// <returns>A tuple containing totalHours and lunchBreakHours.</returns>
        private (double totalHours, double lunchBreakHours) CalculateHoursAndPay(TimeEntry entry)
        {
            try
            {
                if (!entry.TimeIn.HasValue || !entry.TimeOut.HasValue)
                {
                    return (0, 0);
                }

                // Calculate the duration between ClockInTime and ClockOutTime
                double totalHours = SmartTimeHelper.CalculateTotalHours(
                    entry.TimeIn.Value,
                    entry.TimeOut.Value,
                    TimeSpan.FromHours(6), // Lunch deduction threshold
                    TimeSpan.FromMinutes(30) // Lunch deduction duration
                );

                // Determine lunch break deduction
                var rawHours = (entry.TimeOut.Value - entry.TimeIn.Value).TotalHours;
                double lunchBreakHours = rawHours > 6 ? 0.5 : 0; // 30 minutes if over 6 hours

                return (totalHours, lunchBreakHours);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TabletTimeService Error in CalculateHoursAndPay: {ex.Message}");
                return (0, 0);
            }
        }

        /// <summary>
        /// Retrieves a unique identifier for the tablet device.
        /// This could be based on machine name, configuration, or a stored ID.
        /// </summary>
        /// <returns>A string representing the tablet's unique identifier.</returns>
        private string GetTabletIdentifier()
        {
            // In a real application, this should be a more robust and persistent identifier,
            // perhaps stored in application settings or retrieved from a device management API.
            return Environment.MachineName + "_TABLET_DEV"; // Appending "_DEV" for development clarity
        }

        #endregion

        #region Photo Placeholders (Stage 4 Implementation)

        /// <summary>
        /// Captures a photo during clock-in using the PhotoCaptureService.
        /// </summary>
        /// <param name="employeeId">The ID of the employee.</param>
        /// <returns>The file path of the captured photo, or null if not captured.</returns>
        private async Task<string?> CaptureClockInPhotoAsync(int employeeId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"TabletTimeService: Attempting to capture clock-in photo for Employee ID: {employeeId}");
                
                // Use PhotoCaptureService to capture the actual photo
                string? photoPath = await _photoCaptureService.CapturePhotoAsync(employeeId, "ClockIn");
                
                if (photoPath != null)
                {
                    System.Diagnostics.Debug.WriteLine($"TabletTimeService: Clock-in photo captured successfully: {photoPath}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"TabletTimeService: Clock-in photo capture failed for Employee ID: {employeeId} - continuing with clock-in operation");
                }
                
                return photoPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TabletTimeService: Error capturing clock-in photo for Employee ID {employeeId}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"TabletTimeService: Photo capture failure will not prevent clock-in operation from proceeding");
                return null; // Return null on error, but don't throw exception to allow clock-in to proceed
            }
        }

        /// <summary>
        /// Captures a photo during clock-out using the PhotoCaptureService.
        /// </summary>
        /// <param name="employeeId">The ID of the employee.</param>
        /// <returns>The file path of the captured photo, or null if not captured.</returns>
        private async Task<string?> CaptureClockOutPhotoAsync(int employeeId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"TabletTimeService: Attempting to capture clock-out photo for Employee ID: {employeeId}");
                
                // Use PhotoCaptureService to capture the actual photo
                string? photoPath = await _photoCaptureService.CapturePhotoAsync(employeeId, "ClockOut");
                
                if (photoPath != null)
                {
                    System.Diagnostics.Debug.WriteLine($"TabletTimeService: Clock-out photo captured successfully: {photoPath}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"TabletTimeService: Clock-out photo capture failed for Employee ID: {employeeId} - continuing with clock-out operation");
                }
                
                return photoPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TabletTimeService: Error capturing clock-out photo for Employee ID {employeeId}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"TabletTimeService: Photo capture failure will not prevent clock-out operation from proceeding");
                return null; // Return null on error, but don't throw exception to allow clock-out to proceed
            }
        }

        #endregion
    }

    #region Additional Result Classes (if not already defined elsewhere)

    /// <summary>
    /// Represents the current clock status of an employee.
    /// </summary>
    public class EmployeeStatusResult
    {
        public bool Success { get; }
        public string Status { get; } // e.g., "Clocked In", "Available", "Error"
        public DateTime? ClockInTime { get; }
        public TimeSpan? WorkTime { get; }
        public string ErrorMessage { get; }

        public EmployeeStatusResult(bool success, string status, DateTime? clockInTime = null, TimeSpan? workTime = null, string errorMessage = "")
        {
            Success = success;
            Status = status ?? string.Empty;
            ClockInTime = clockInTime;
            WorkTime = workTime;
            ErrorMessage = errorMessage ?? string.Empty;
        }
    }

    #endregion
}
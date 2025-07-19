#nullable enable
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EmployeeTimeTracker.Models;
using EmployeeTimeTracker.Data;
using Microsoft.Extensions.Logging;

namespace EmployeeTimeTrackerTablet.Views
{
    /// <summary>
    /// Interaction logic for DualTimeCorrectionDialog.xaml
    /// Provides manager interface for correcting both clock-in and clock-out times.
    /// </summary>
    public partial class DualTimeCorrectionDialog : Window
    {
        private readonly Employee _employee;
        private readonly TimeEntry _timeEntry;
        private readonly TimeEntryRepository _timeEntryRepository;
        private readonly ILogger<DualTimeCorrectionDialog> _logger;
        
        private DateTime _originalClockInDateTime;
        private DateTime? _originalClockOutDateTime;
        private DateTime? _correctedClockInDateTime;
        private DateTime? _correctedClockOutDateTime;

        /// <summary>
        /// Gets the corrected clock-in time selected by the manager.
        /// </summary>
        public DateTime? CorrectedClockInTime => _correctedClockInDateTime;

        /// <summary>
        /// Gets the corrected clock-out time selected by the manager.
        /// </summary>
        public DateTime? CorrectedClockOutTime => _correctedClockOutDateTime;

        /// <summary>
        /// Gets the correction reason entered by the manager.
        /// </summary>
        public string CorrectionReason => CorrectionReasonTextBox.Text?.Trim() ?? string.Empty;

        /// <summary>
        /// Gets whether any corrections were applied successfully.
        /// </summary>
        public bool IsApplied { get; private set; } = false;

        /// <summary>
        /// Gets whether clock-in correction is enabled.
        /// </summary>
        public bool IsClockInCorrectionEnabled => EnableClockInCorrection.IsChecked == true;

        /// <summary>
        /// Gets whether clock-out correction is enabled.
        /// </summary>
        public bool IsClockOutCorrectionEnabled => EnableClockOutCorrection.IsChecked == true;

        /// <summary>
        /// Initializes a new instance of the DualTimeCorrectionDialog.
        /// </summary>
        /// <param name="employee">The employee whose time is being corrected.</param>
        /// <param name="timeEntry">The time entry to be corrected.</param>
        /// <param name="timeEntryRepository">Repository for time entry operations.</param>
        /// <param name="logger">Logger instance for diagnostics.</param>
        public DualTimeCorrectionDialog(Employee employee, TimeEntry timeEntry, 
            TimeEntryRepository timeEntryRepository, ILogger<DualTimeCorrectionDialog> logger)
        {
            InitializeComponent();
            
            _employee = employee ?? throw new ArgumentNullException(nameof(employee));
            _timeEntry = timeEntry ?? throw new ArgumentNullException(nameof(timeEntry));
            _timeEntryRepository = timeEntryRepository ?? throw new ArgumentNullException(nameof(timeEntryRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Calculate original DateTime using actual property names
            _originalClockInDateTime = _timeEntry.ShiftDate.Date.Add(_timeEntry.TimeIn ?? TimeSpan.Zero);
            _originalClockOutDateTime = _timeEntry.TimeOut.HasValue 
                ? _timeEntry.ShiftDate.Date.Add(_timeEntry.TimeOut.Value) 
                : null;
            
            InitializeDialog();
            PopulateTimeComboBoxes();
            
            _logger.LogInformation($"DualTimeCorrectionDialog initialized for {_employee.FirstName} {_employee.LastName}");
        }

        /// <summary>
        /// Initializes the dialog with employee and time entry information.
        /// </summary>
        private void InitializeDialog()
        {
            // Set employee information using actual property names
            EmployeeNameText.Text = $"{_employee.FirstName} {_employee.LastName}";
            JobTitleText.Text = _employee.JobTitle ?? "Not specified";
            EmployeeIdText.Text = _employee.EmployeeID.ToString();
            ShiftDateText.Text = _timeEntry.ShiftDate.ToString("dddd, MMMM dd, yyyy");
            
            // Set hourly rate and department - these controls will be uncommented when added to XAML
            // HourlyRateText.Text = $"${_employee.PayRate:F2}/hour";
            // DepartmentText.Text = "General"; // Default since not in current schema

            // Set current times
            CurrentClockInText.Text = _originalClockInDateTime.ToString("h:mm tt");
            CurrentClockOutText.Text = _originalClockOutDateTime?.ToString("h:mm tt") ?? "Not clocked out";

            // Calculate and display current totals
            UpdateCurrentTotals();

            // Initialize date pickers with current shift date
            ClockInDatePicker.SelectedDate = _timeEntry.ShiftDate;
            ClockOutDatePicker.SelectedDate = _timeEntry.ShiftDate;

            // Set default correction times to current times
            SetClockInTimeControls(_originalClockInDateTime);
            if (_originalClockOutDateTime.HasValue)
            {
                SetClockOutTimeControls(_originalClockOutDateTime.Value);
            }

            // Update header with employee info
            EmployeeInfoText.Text = $"Correct times for {_employee.FirstName} {_employee.LastName} - {_timeEntry.ShiftDate:MMM dd, yyyy}";
            
            // ENHANCEMENT: Pre-populate correction reason with professional template
            InitializeCorrectionReasonTemplate();
        }

        /// <summary>
        /// Initializes the correction reason field with a simple, clean template.
        /// Includes only manager name and current date/time as requested.
        /// </summary>
        private void InitializeCorrectionReasonTemplate()
        {
            try
            {
                var currentDateTime = DateTime.Now;
                var managerName = Environment.UserName; // Get current Windows user
                
                // Simple, clean template with just manager name and date/time
                var simpleTemplate = $"Manager Name: {managerName}\n" +
                                   $"Date/Time: {currentDateTime:yyyy-MM-dd HH:mm}\n" +
                                   $"Reason: ";

                // Set the simple template in the text box
                CorrectionReasonTextBox.Text = simpleTemplate;
                
                // Position cursor at the end for easy editing
                CorrectionReasonTextBox.Focus();
                CorrectionReasonTextBox.SelectionStart = simpleTemplate.Length;
                
                _logger.LogInformation("Simple correction reason template initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize correction reason template, using fallback");
                
                // Fallback to even simpler template if anything fails
                var fallbackTemplate = $"Manager: {Environment.UserName} | {DateTime.Now:yyyy-MM-dd HH:mm}\nReason: ";
                
                CorrectionReasonTextBox.Text = fallbackTemplate;
                CorrectionReasonTextBox.SelectionStart = fallbackTemplate.Length;
            }
        }

        /// <summary>
        /// Populates the hour, minute, and AM/PM combo boxes.
        /// FIXED: Changed minute increments from 15-minute to 5-minute intervals.
        /// </summary>
        private void PopulateTimeComboBoxes()
        {
            // Populate hours (1-12)
            for (int hour = 1; hour <= 12; hour++)
            {
                ClockInHourComboBox.Items.Add(hour.ToString());
                ClockOutHourComboBox.Items.Add(hour.ToString());
            }

            // FIXED: Populate minutes in 5-minute increments (00, 05, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55)
            for (int minute = 0; minute < 60; minute += 5)
            {
                string minuteString = minute.ToString("00"); // Ensure two-digit format
                ClockInMinuteComboBox.Items.Add(minuteString);
                ClockOutMinuteComboBox.Items.Add(minuteString);
            }

            // Populate AM/PM
            ClockInAmPmComboBox.Items.Add("AM");
            ClockInAmPmComboBox.Items.Add("PM");
            ClockOutAmPmComboBox.Items.Add("AM");
            ClockOutAmPmComboBox.Items.Add("PM");
        }

        /// <summary>
        /// Sets the clock-in time controls to the specified time.
        /// FIXED: Updated to work with 5-minute increments.
        /// </summary>
        /// <param name="dateTime">The DateTime to set.</param>
        private void SetClockInTimeControls(DateTime dateTime)
        {
            int hour = dateTime.Hour;
            int minute = dateTime.Minute;
            bool isPM = hour >= 12;

            // Convert to 12-hour format
            if (hour == 0) hour = 12;
            else if (hour > 12) hour -= 12;
            ClockInHourComboBox.SelectedItem = hour.ToString();

            // FIXED: Round minutes to nearest 5-minute interval
            int roundedMinute = ((minute + 2) / 5) * 5; // Round to nearest 5 minutes. +2 helps in rounding correctly.
            if (roundedMinute >= 60) roundedMinute = 0; // Handle rounding up to 60 (next hour)

            ClockInMinuteComboBox.SelectedItem = roundedMinute.ToString("00");

            ClockInAmPmComboBox.SelectedItem = isPM ? "PM" : "AM";
        }

        /// <summary>
        /// Sets the clock-out time controls to the specified time.
        /// FIXED: Updated to work with 5-minute increments.
        /// </summary>
        /// <param name="dateTime">The DateTime to set.</param>
        private void SetClockOutTimeControls(DateTime dateTime)
        {
            int hour = dateTime.Hour;
            int minute = dateTime.Minute;
            bool isPM = hour >= 12;

            // Convert to 12-hour format
            if (hour == 0) hour = 12;
            else if (hour > 12) hour -= 12;
            ClockOutHourComboBox.SelectedItem = hour.ToString();

            // FIXED: Round minutes to nearest 5-minute interval
            int roundedMinute = ((minute + 2) / 5) * 5; // Round to nearest 5 minutes. +2 helps in rounding correctly.
            if (roundedMinute >= 60) roundedMinute = 0; // Handle rounding up to 60 (next hour)

            ClockOutMinuteComboBox.SelectedItem = roundedMinute.ToString("00");

            ClockOutAmPmComboBox.SelectedItem = isPM ? "PM" : "AM";
        }

        /// <summary>
        /// Updates the current totals display.
        /// </summary>
        private void UpdateCurrentTotals()
        {
            if (_originalClockOutDateTime.HasValue)
            {
                TimeSpan duration = _originalClockOutDateTime.Value - _originalClockInDateTime;
                double hours = duration.TotalHours;
                double pay = hours * (double)_employee.PayRate; // Convert decimal to double

                CurrentTotalHoursText.Text = $"{hours:F2} hours";
                CurrentTotalPayText.Text = $"${pay:F2}";
            }
            else
            {
                CurrentTotalHoursText.Text = "Not available";
                CurrentTotalPayText.Text = "Not available";
            }
        }

        /// <summary>
        /// Event handler for clock-in correction checkbox.
        /// </summary>
        private void EnableClockInCorrection_Checked(object sender, RoutedEventArgs e)
        {
            ClockInCorrectionPanel.IsEnabled = true;
            // ClockInStatusText.Text = "Clock-In: Enabled for Correction"; // Uncomment when control is added to XAML
            UpdateCorrectedTimes();
            ValidateForm();
        }

        /// <summary>
        /// Event handler for clock-in correction checkbox unchecked.
        /// </summary>
        private void EnableClockInCorrection_Unchecked(object sender, RoutedEventArgs e)
        {
            ClockInCorrectionPanel.IsEnabled = false;
            // ClockInStatusText.Text = "Clock-In: Not Modified"; // Uncomment when control is added to XAML
            _correctedClockInDateTime = null;
            NewClockInText.Text = "Not set";
            UpdateCorrectionSummary();
            ValidateForm();
        }

        /// <summary>
        /// Event handler for clock-out correction checkbox.
        /// </summary>
        private void EnableClockOutCorrection_Checked(object sender, RoutedEventArgs e)
        {
            ClockOutCorrectionPanel.IsEnabled = true;
            // ClockOutStatusText.Text = "Clock-Out: Enabled for Correction"; // Uncomment when control is added to XAML
            UpdateCorrectedTimes();
            ValidateForm();
        }

        /// <summary>
        /// Event handler for clock-out correction checkbox unchecked.
        /// </summary>
        private void EnableClockOutCorrection_Unchecked(object sender, RoutedEventArgs e)
        {
            ClockOutCorrectionPanel.IsEnabled = false;
            // ClockOutStatusText.Text = "Clock-Out: Not Modified"; // Uncomment when control is added to XAML
            _correctedClockOutDateTime = null;
            NewClockOutText.Text = "Not set";
            UpdateCorrectionSummary();
            ValidateForm();
        }

        /// <summary>
        /// Event handler for clock-in date picker changes.
        /// </summary>
        private void ClockInDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCorrectedTimes();
            ValidateForm();
        }

        /// <summary>
        /// Event handler for clock-out date picker changes.
        /// </summary>
        private void ClockOutDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCorrectedTimes();
            ValidateForm();
        }

        /// <summary>
        /// Event handler for clock-in time selection changes.
        /// </summary>
        private void ClockInTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCorrectedTimes();
            ValidateForm();
        }

        /// <summary>
        /// Event handler for clock-out time selection changes.
        /// </summary>
        private void ClockOutTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCorrectedTimes();
            ValidateForm();
        }

        /// <summary>
        /// Event handler for correction reason text changes.
        /// </summary>
        private void CorrectionReasonTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Update character count - uncomment when control is added to XAML
            int charCount = CorrectionReasonTextBox.Text?.Length ?? 0;
            // CharacterCountText.Text = $"{charCount}/500 characters";
            
            // Change color based on length - uncomment when control is added to XAML
            // if (charCount > 450)
            //     CharacterCountText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
            // else if (charCount > 400)
            //     CharacterCountText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
            // else
            //     CharacterCountText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
            
            ValidateForm();
        }

        /// <summary>
        /// Updates the corrected times based on current control values.
        /// </summary>
        private void UpdateCorrectedTimes()
        {
            // Update clock-in correction
            if (EnableClockInCorrection.IsChecked == true)
            {
                _correctedClockInDateTime = GetClockInCorrectedDateTime();
                NewClockInText.Text = _correctedClockInDateTime?.ToString("dddd, MMM dd, yyyy h:mm tt") ?? "Invalid time";
            }

            // Update clock-out correction
            if (EnableClockOutCorrection.IsChecked == true)
            {
                _correctedClockOutDateTime = GetClockOutCorrectedDateTime();
                NewClockOutText.Text = _correctedClockOutDateTime?.ToString("dddd, MMM dd, yyyy h:mm tt") ?? "Invalid time";
            }

            UpdateCorrectionSummary();
        }

        /// <summary>
        /// Gets the corrected clock-in DateTime from the controls.
        /// </summary>
        /// <returns>The corrected clock-in DateTime, or null if invalid.</returns>
        private DateTime? GetClockInCorrectedDateTime()
        {
            if (ClockInDatePicker.SelectedDate == null ||
                ClockInHourComboBox.SelectedItem == null ||
                ClockInMinuteComboBox.SelectedItem == null ||
                ClockInAmPmComboBox.SelectedItem == null)
            {
                return null;
            }

            var hourString = ClockInHourComboBox.SelectedItem.ToString();
            var minuteString = ClockInMinuteComboBox.SelectedItem.ToString();
            var amPmString = ClockInAmPmComboBox.SelectedItem.ToString();

            if (hourString == null || minuteString == null || amPmString == null)
            {
                return null;
            }

            if (!int.TryParse(hourString, out int hour) || !int.TryParse(minuteString, out int minute))
            {
                return null;
            }

            bool isPM = amPmString == "PM";

            // Convert to 24-hour format
            if (isPM && hour != 12) hour += 12;
            else if (!isPM && hour == 12) hour = 0;

            return ClockInDatePicker.SelectedDate.Value.Date.AddHours(hour).AddMinutes(minute);
        }

        /// <summary>
        /// Gets the corrected clock-out DateTime from the controls.
        /// </summary>
        /// <returns>The corrected clock-out DateTime, or null if invalid.</returns>
        private DateTime? GetClockOutCorrectedDateTime()
        {
            if (ClockOutDatePicker.SelectedDate == null ||
                ClockOutHourComboBox.SelectedItem == null ||
                ClockOutMinuteComboBox.SelectedItem == null ||
                ClockOutAmPmComboBox.SelectedItem == null)
            {
                return null;
            }

            var hourString = ClockOutHourComboBox.SelectedItem.ToString();
            var minuteString = ClockOutMinuteComboBox.SelectedItem.ToString();
            var amPmString = ClockOutAmPmComboBox.SelectedItem.ToString();

            if (hourString == null || minuteString == null || amPmString == null)
            {
                return null;
            }

            if (!int.TryParse(hourString, out int hour) || !int.TryParse(minuteString, out int minute))
            {
                return null;
            }

            bool isPM = amPmString == "PM";

            // Convert to 24-hour format
            if (isPM && hour != 12) hour += 12;
            else if (!isPM && hour == 12) hour = 0;

            return ClockOutDatePicker.SelectedDate.Value.Date.AddHours(hour).AddMinutes(minute);
        }

        /// <summary>
        /// Updates the correction summary display.
        /// </summary>
        private void UpdateCorrectionSummary()
        {
            DateTime effectiveClockIn = _correctedClockInDateTime ?? _originalClockInDateTime;
            DateTime? effectiveClockOut = _correctedClockOutDateTime ?? _originalClockOutDateTime;

            if (effectiveClockOut.HasValue)
            {
                TimeSpan duration = effectiveClockOut.Value - effectiveClockIn;
                double newHours = duration.TotalHours;
                
                // Handle potential negative hours display
                if (newHours < 0)
                {
                    NewTotalHoursText.Text = $"{newHours:F2} hours (Invalid - Negative Time)";
                    NewTotalPayText.Text = "Invalid";
                    PayDifferenceText.Text = "Invalid";
                    return;
                }
                
                double newPay = newHours * (double)_employee.PayRate;

                NewTotalHoursText.Text = $"{newHours:F2} hours";
                NewTotalPayText.Text = $"${newPay:F2}";

                // Calculate difference
                if (_originalClockOutDateTime.HasValue)
                {
                    TimeSpan originalDuration = _originalClockOutDateTime.Value - _originalClockInDateTime;
                    double originalPay = originalDuration.TotalHours * (double)_employee.PayRate;
                    double payDifference = newPay - originalPay;

                    PayDifferenceText.Text = payDifference >= 0 ? $"+${payDifference:F2}" : $"-${Math.Abs(payDifference):F2}";
                }
                else
                {
                    PayDifferenceText.Text = $"+${newPay:F2}";
                }
            }
            else
            {
                NewTotalHoursText.Text = "Not available";
                NewTotalPayText.Text = "Not available";
                PayDifferenceText.Text = "Not available";
            }
        }

        /// <summary>
        /// Validates the form and enables/disables the Apply button.
        /// </summary>
        private void ValidateForm()
        {
            ClearMessages();

            // Check if at least one correction is enabled
            bool hasCorrection = EnableClockInCorrection.IsChecked == true || EnableClockOutCorrection.IsChecked == true;
            
            if (!hasCorrection)
            {
                ApplyButton.IsEnabled = false;
                return;
            }

            // Simplified validation for correction reason
            string? reason = CorrectionReasonTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(reason))
            {
                ShowErrorMessage("Correction reason is required for audit trail.");
                ApplyButton.IsEnabled = false;
                return;
            }

            // Extract meaningful content after "Reason:" in the template
            string meaningfulContent = "";
            
            // Look for content after "Reason:" prompt
            int reasonIndex = reason.IndexOf("Reason:");
            if (reasonIndex >= 0)
            {
                meaningfulContent = reason.Substring(reasonIndex + 7).Trim(); // Skip "Reason:" (7 chars)
            }
            else
            {
                // Fallback: check for content beyond template lines
                var lines = reason.Split('\n');
                foreach (var line in lines)
                {
                    // Skip template lines (Manager Name and Date/Time)
                    if (!line.StartsWith("Manager Name:") && 
                        !line.StartsWith("Date/Time:") && 
                        !line.StartsWith("Manager:") &&
                        !string.IsNullOrWhiteSpace(line))
                    {
                        meaningfulContent += line.Trim() + " ";
                    }
                }
                meaningfulContent = meaningfulContent.Trim();
            }
            
            // Require at least 3 characters for the actual reason
            if (string.IsNullOrEmpty(meaningfulContent) || meaningfulContent.Length < 3)
            {
                ShowErrorMessage("Please provide a reason for the time correction after 'Reason:' (minimum 3 characters).");
                ApplyButton.IsEnabled = false;
                return;
            }

            // Validate clock-in correction if enabled
            if (EnableClockInCorrection.IsChecked == true)
            {
                var clockInTime = GetClockInCorrectedDateTime();
                if (!clockInTime.HasValue)
                {
                    ShowErrorMessage("Invalid clock-in correction time.");
                    ApplyButton.IsEnabled = false;
                    return;
                }

                // Check if clock-in time is in the future
                if (clockInTime.Value > DateTime.Now)
                {
                    ShowErrorMessage("Clock-in time cannot be in the future.");
                    ApplyButton.IsEnabled = false;
                    return;
                }
            }

            // Validate clock-out correction if enabled
            if (EnableClockOutCorrection.IsChecked == true)
            {
                var clockOutTime = GetClockOutCorrectedDateTime();
                if (!clockOutTime.HasValue)
                {
                    ShowErrorMessage("Invalid clock-out correction time.");
                    ApplyButton.IsEnabled = false;
                    return;
                }

                // Check if clock-out time is in the future
                if (clockOutTime.Value > DateTime.Now)
                {
                    ShowErrorMessage("Clock-out time cannot be in the future.");
                    ApplyButton.IsEnabled = false;
                    return;
                }

                // Check if clock-out is after clock-in
                DateTime effectiveClockIn = _correctedClockInDateTime ?? _originalClockInDateTime;
                if (clockOutTime.Value <= effectiveClockIn)
                {
                    ShowErrorMessage("Clock-out time must be after clock-in time.");
                    ApplyButton.IsEnabled = false;
                    return;
                }
            }

            // If both corrections are enabled, validate the sequence
            if (EnableClockInCorrection.IsChecked == true && EnableClockOutCorrection.IsChecked == true)
            {
                var clockInTime = GetClockInCorrectedDateTime();
                var clockOutTime = GetClockOutCorrectedDateTime();

                if (clockInTime.HasValue && clockOutTime.HasValue && clockOutTime.Value <= clockInTime.Value)
                {
                    ShowErrorMessage("Clock-out time must be after clock-in time.");
                    ApplyButton.IsEnabled = false;
                    return;
                }
            }

            // NEW VALIDATION: Check for negative total hours
            DateTime validationClockIn = _correctedClockInDateTime ?? _originalClockInDateTime;
            DateTime? validationClockOut = _correctedClockOutDateTime ?? _originalClockOutDateTime;

            if (validationClockOut.HasValue)
            {
                TimeSpan duration = validationClockOut.Value - validationClockIn;
                double totalHours = duration.TotalHours;

                if (totalHours <= 0)
                {
                    ShowErrorMessage("Total hours cannot be zero or negative. Please ensure clock-out time is after clock-in time.");
                    ApplyButton.IsEnabled = false;
                    return;
                }
            }

            // All validations passed
            ApplyButton.IsEnabled = true;
        }

        /// <summary>
        /// Event handler for Apply button click.
        /// </summary>
        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplyButton.IsEnabled = false;
                ShowStatusMessage("Applying corrections...");

                // Apply the corrections
                bool success = await ApplyCorrections();

                if (success)
                {
                    IsApplied = true;
                    ShowStatusMessage("Corrections applied successfully!");
                    
                    _logger.LogInformation($"Time corrections applied successfully for employee {_employee.EmployeeID}");
                    
                    // Close dialog after a brief delay
                    await System.Threading.Tasks.Task.Delay(1500);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    ShowErrorMessage("Failed to apply corrections. Please try again.");
                    ApplyButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying time corrections");
                ShowErrorMessage($"Error applying corrections: {ex.Message}");
                ApplyButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Applies the time corrections to the database.
        /// </summary>
        /// <returns>True if successful, false otherwise.</returns>
        private async System.Threading.Tasks.Task<bool> ApplyCorrections()
        {
            try
            {
                // FIXED: Ensure corrected times are properly applied to TimeEntry properties
                
                // Update TimeIn if clock-in correction is enabled
                if (EnableClockInCorrection.IsChecked == true && _correctedClockInDateTime.HasValue)
                {
                    _timeEntry.TimeIn = _correctedClockInDateTime.Value.TimeOfDay;
                    // If the date was also corrected, update ShiftDate
                    if (_correctedClockInDateTime.Value.Date != _timeEntry.ShiftDate.Date)
                    {
                        _timeEntry.ShiftDate = _correctedClockInDateTime.Value.Date;
                    }
                }

                // Update TimeOut if clock-out correction is enabled
                if (EnableClockOutCorrection.IsChecked == true && _correctedClockOutDateTime.HasValue)
                {
                    _timeEntry.TimeOut = _correctedClockOutDateTime.Value.TimeOfDay;
                }

                // Create updated time entry with the corrected values
                var updatedTimeEntry = new TimeEntry
                {
                    EntryID = _timeEntry.EntryID,
                    EmployeeID = _timeEntry.EmployeeID,
                    ShiftDate = _timeEntry.ShiftDate,
                    TimeIn = _timeEntry.TimeIn,
                    TimeOut = _timeEntry.TimeOut,
                    ModifiedDate = DateTime.Now
                };

                // Recalculate totals based on the updated times
                if (updatedTimeEntry.TimeIn.HasValue && updatedTimeEntry.TimeOut.HasValue)
                {
                    var duration = updatedTimeEntry.TimeOut.Value - updatedTimeEntry.TimeIn.Value;
                    // Handle day rollover if clock-out is on the next day
                    if (duration.TotalHours < 0)
                    {
                        duration = duration.Add(TimeSpan.FromDays(1));
                    }
                    updatedTimeEntry.TotalHours = (decimal)duration.TotalHours;
                    updatedTimeEntry.GrossPay = updatedTimeEntry.TotalHours * _employee.PayRate;
                }
                else if (updatedTimeEntry.TimeIn.HasValue && !updatedTimeEntry.TimeOut.HasValue)
                {
                    // Reset totals for ongoing shift
                    updatedTimeEntry.TotalHours = 0;
                    updatedTimeEntry.GrossPay = 0;
                }

                // Add correction audit trail to notes
                string corrections = "";
                if (EnableClockInCorrection.IsChecked == true)
                {
                    corrections += $"Clock-in: {_originalClockInDateTime:g} ? {_correctedClockInDateTime:g}; ";
                }
                if (EnableClockOutCorrection.IsChecked == true)
                {
                    corrections += $"Clock-out: {_originalClockOutDateTime:g} ? {_correctedClockOutDateTime:g}; ";
                }

                updatedTimeEntry.Notes = (_timeEntry.Notes ?? "") + 
                    $" | MANAGER CORRECTION: {DateTime.Now:yyyy-MM-dd HH:mm} - {corrections}Reason: {CorrectionReason}";

                // Update in database using actual repository method
                var result = await _timeEntryRepository.UpdateAsync(updatedTimeEntry);

                if (result)
                {
                    _logger.LogInformation($"Manager time correction applied - Employee: {_employee.EmployeeID}, " +
                                           $"Corrections: {corrections}, Reason: {CorrectionReason}");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"Failed to update time entry in database for employee {_employee.EmployeeID}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply time corrections to database");
                return false;
            }
        }

        /// <summary>
        /// Event handler for Cancel button click.
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Shows an error message to the user.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        private void ShowErrorMessage(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
            StatusMessage.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Shows a status message to the user.
        /// </summary>
        /// <param name="message">The status message to display.</param>
        private void ShowStatusMessage(string message)
        {
            StatusMessage.Text = message;
            StatusMessage.Visibility = Visibility.Visible;
            ErrorMessage.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Clears all messages.
        /// </summary>
        private void ClearMessages()
        {
            ErrorMessage.Visibility = Visibility.Collapsed;
            StatusMessage.Visibility = Visibility.Collapsed;
        }
    }
}
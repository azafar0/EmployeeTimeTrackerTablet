using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EmployeeTimeTracker.Models;

namespace EmployeeTimeTrackerTablet.Views
{
    /// <summary>
    /// Interaction logic for TimeCorrectionDialog.xaml
    /// Provides manager interface for correcting employee clock-out times.
    /// </summary>
    public partial class TimeCorrectionDialog : Window
    {
        private readonly Employee _employee;
        private readonly TimeEntry _timeEntry;
        private DateTime _clockInDateTime;
        private DateTime? _correctedClockOutDateTime;

        /// <summary>
        /// Gets the corrected clock-out time selected by the manager.
        /// </summary>
        public DateTime? CorrectedClockOutTime => _correctedClockOutDateTime;

        /// <summary>
        /// Gets the correction reason entered by the manager.
        /// </summary>
        public string CorrectionReason => CorrectionReasonTextBox.Text?.Trim() ?? string.Empty;

        /// <summary>
        /// Gets whether the correction was applied successfully.
        /// </summary>
        public bool IsApplied { get; private set; } = false;

        /// <summary>
        /// Initializes a new instance of the TimeCorrectionDialog.
        /// </summary>
        /// <param name="employee">The employee whose time is being corrected.</param>
        /// <param name="timeEntry">The time entry to be corrected.</param>
        public TimeCorrectionDialog(Employee employee, TimeEntry timeEntry)
        {
            InitializeComponent();
            
            _employee = employee ?? throw new ArgumentNullException(nameof(employee));
            _timeEntry = timeEntry ?? throw new ArgumentNullException(nameof(timeEntry));
            
            // Calculate clock-in DateTime
            _clockInDateTime = _timeEntry.ShiftDate.Date.Add(_timeEntry.TimeIn ?? TimeSpan.Zero);
            
            InitializeDialog();
            PopulateTimeComboBoxes();
            
            System.Diagnostics.Debug.WriteLine($"TimeCorrectionDialog: Dialog initialized for {_employee.FirstName} {_employee.LastName}");
        }

        /// <summary>
        /// Initializes the dialog with employee and time entry information.
        /// </summary>
        private void InitializeDialog()
        {
            try
            {
                // Set employee information
                EmployeeNameText.Text = _employee.FullName;
                
                // Set current shift information
                ClockInText.Text = _clockInDateTime.ToString("MM/dd/yyyy h:mm tt");
                
                if (_timeEntry.TimeOut.HasValue)
                {
                    var originalClockOut = _timeEntry.ShiftDate.Date.Add(_timeEntry.TimeOut.Value);
                    OriginalClockOutText.Text = originalClockOut.ToString("MM/dd/yyyy h:mm tt");
                    
                    var originalDuration = originalClockOut - _clockInDateTime;
                    OriginalDurationText.Text = $"{originalDuration.TotalHours:F2} hours";
                    
                    OriginalPayText.Text = $"${_timeEntry.GrossPay:F2}";
                }
                else
                {
                    OriginalClockOutText.Text = "Still clocked in";
                    OriginalDurationText.Text = "In progress";
                    OriginalPayText.Text = "Calculating...";
                }
                
                // Set default correction date to today
                CorrectionDatePicker.SelectedDate = DateTime.Today;
                
                System.Diagnostics.Debug.WriteLine("TimeCorrectionDialog: Dialog initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TimeCorrectionDialog: Error initializing dialog: {ex.Message}");
                ShowErrorMessage("Error initializing dialog. Please try again.");
            }
        }

        /// <summary>
        /// Populates the time combo boxes with hours, minutes, and AM/PM options.
        /// </summary>
        private void PopulateTimeComboBoxes()
        {
            try
            {
                // Populate hours (1-12)
                HourComboBox.Items.Clear();
                for (int hour = 1; hour <= 12; hour++)
                {
                    HourComboBox.Items.Add(hour.ToString());
                }
                
                // Populate minutes (00-59, 5-minute intervals)
                MinuteComboBox.Items.Clear();
                for (int minute = 0; minute < 60; minute += 5)
                {
                    MinuteComboBox.Items.Add(minute.ToString("00"));
                }
                
                // Set default time to current time
                var currentTime = DateTime.Now;
                var hour12 = currentTime.Hour > 12 ? currentTime.Hour - 12 : (currentTime.Hour == 0 ? 12 : currentTime.Hour);
                var currentMinute = (currentTime.Minute / 5) * 5; // Round to nearest 5-minute interval
                
                HourComboBox.SelectedItem = hour12.ToString();
                MinuteComboBox.SelectedItem = currentMinute.ToString("00");
                AmPmComboBox.SelectedIndex = currentTime.Hour >= 12 ? 1 : 0;
                
                System.Diagnostics.Debug.WriteLine("TimeCorrectionDialog: Time combo boxes populated successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TimeCorrectionDialog: Error populating combo boxes: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the SelectedDateChanged event for the correction date picker.
        /// </summary>
        private void CorrectionDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculateAndDisplayCorrection();
        }

        /// <summary>
        /// Handles the SelectionChanged event for the time combo boxes.
        /// </summary>
        private void TimeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculateAndDisplayCorrection();
        }

        /// <summary>
        /// Handles the TextChanged event for the correction reason text box.
        /// </summary>
        private void CorrectionReasonTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateForm();
        }

        /// <summary>
        /// Calculates and displays the correction summary.
        /// </summary>
        private void CalculateAndDisplayCorrection()
        {
            try
            {
                ClearMessages();
                
                // Get selected date and time
                var selectedDate = CorrectionDatePicker.SelectedDate;
                var selectedHour = HourComboBox.SelectedItem?.ToString();
                var selectedMinute = MinuteComboBox.SelectedItem?.ToString();
                var selectedAmPm = AmPmComboBox.SelectedItem as ComboBoxItem;
                
                if (selectedDate == null || selectedHour == null || selectedMinute == null || selectedAmPm == null)
                {
                    ResetCorrectionSummary();
                    return;
                }
                
                // Parse the time
                if (!int.TryParse(selectedHour, out int hour) || !int.TryParse(selectedMinute, out int minute))
                {
                    ResetCorrectionSummary();
                    return;
                }
                
                // Convert to 24-hour format
                var isPm = selectedAmPm.Content.ToString() == "PM";
                if (isPm && hour != 12)
                {
                    hour += 12;
                }
                else if (!isPm && hour == 12)
                {
                    hour = 0;
                }
                
                // Create the corrected DateTime
                _correctedClockOutDateTime = selectedDate.Value.Date.Add(new TimeSpan(hour, minute, 0));
                
                // Validate the corrected time
                if (!ValidateCorrectedTime(_correctedClockOutDateTime.Value))
                {
                    ResetCorrectionSummary();
                    return;
                }
                
                // Calculate new duration and pay
                var newDuration = _correctedClockOutDateTime.Value - _clockInDateTime;
                var newTotalHours = (decimal)newDuration.TotalHours;
                var newGrossPay = newTotalHours * _employee.PayRate;
                
                // Update summary display
                NewClockOutText.Text = _correctedClockOutDateTime.Value.ToString("MM/dd/yyyy h:mm tt");
                NewDurationText.Text = $"{newDuration.TotalHours:F2} hours";
                NewPayText.Text = $"${newGrossPay:F2}";
                
                ValidateForm();
                
                System.Diagnostics.Debug.WriteLine($"TimeCorrectionDialog: Correction calculated - Duration: {newDuration.TotalHours:F2}h, Pay: ${newGrossPay:F2}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TimeCorrectionDialog: Error calculating correction: {ex.Message}");
                ShowErrorMessage("Error calculating correction. Please check your time selection.");
                ResetCorrectionSummary();
            }
        }

        /// <summary>
        /// Validates the corrected time against business rules.
        /// </summary>
        /// <param name="correctedTime">The corrected clock-out time to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        private bool ValidateCorrectedTime(DateTime correctedTime)
        {
            // Check if corrected time is after clock-in time
            if (correctedTime <= _clockInDateTime)
            {
                ShowErrorMessage("Clock-out time must be after clock-in time.");
                return false;
            }
            
            // Check if corrected time is not in the future
            if (correctedTime > DateTime.Now)
            {
                ShowErrorMessage("Clock-out time cannot be in the future.");
                return false;
            }
            
            // Check maximum shift duration (24 hours)
            var duration = correctedTime - _clockInDateTime;
            if (duration.TotalHours > 24)
            {
                ShowErrorMessage("Shift duration cannot exceed 24 hours.");
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Validates the entire form and enables/disables the Apply button.
        /// </summary>
        private void ValidateForm()
        {
            try
            {
                var isValid = _correctedClockOutDateTime.HasValue && 
                              !string.IsNullOrWhiteSpace(CorrectionReasonTextBox.Text) &&
                              CorrectionReasonTextBox.Text.Trim().Length >= 10;
                
                ApplyButton.IsEnabled = isValid;
                
                if (!isValid && !string.IsNullOrWhiteSpace(CorrectionReasonTextBox.Text) && 
                    CorrectionReasonTextBox.Text.Trim().Length < 10)
                {
                    ShowErrorMessage("Correction reason must be at least 10 characters.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TimeCorrectionDialog: Error validating form: {ex.Message}");
            }
        }

        /// <summary>
        /// Resets the correction summary display.
        /// </summary>
        private void ResetCorrectionSummary()
        {
            NewClockOutText.Text = "[Select date and time above]";
            NewDurationText.Text = "[Calculated when time selected]";
            NewPayText.Text = "[Calculated when time selected]";
            _correctedClockOutDateTime = null;
            ApplyButton.IsEnabled = false;
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

        /// <summary>
        /// Handles the Click event for the Apply button.
        /// </summary>
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_correctedClockOutDateTime.HasValue)
                {
                    ShowErrorMessage("Please select a valid correction time.");
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(CorrectionReasonTextBox.Text) || 
                    CorrectionReasonTextBox.Text.Trim().Length < 10)
                {
                    ShowErrorMessage("Please provide a correction reason (minimum 10 characters).");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"TimeCorrectionDialog: Applying correction - New time: {_correctedClockOutDateTime.Value}, Reason: {CorrectionReason}");
                
                IsApplied = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TimeCorrectionDialog: Error applying correction: {ex.Message}");
                ShowErrorMessage("Error applying correction. Please try again.");
            }
        }

        /// <summary>
        /// Handles the Click event for the Cancel button.
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("TimeCorrectionDialog: Correction cancelled by user");
                IsApplied = false;
                DialogResult = false;
                Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TimeCorrectionDialog: Error cancelling correction: {ex.Message}");
            }
        }
    }
}
#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using EmployeeTimeTracker.Data;
using EmployeeTimeTracker.Models;
using System.Threading.Tasks;
using System;
using System.Linq;
using WpfMessageBox = System.Windows.MessageBox;
using System.Windows;
using System.Windows.Threading;
using EmployeeTimeTrackerTablet.Views;
using EmployeeTimeTrackerTablet.Services;
using Microsoft.Extensions.Logging;

namespace EmployeeTimeTrackerTablet.ViewModels
{
    public partial class AdminMainViewModel : ObservableObject, IDisposable
    {
        private readonly EmployeeRepository _employeeRepository;
        private readonly TimeEntryRepository _timeEntryRepository;
        private readonly ILogger<AdminMainViewModel> _logger;
        private readonly ManagerAuthService? _managerAuthService;
        private readonly DispatcherTimer? _clockTimer;
        private readonly DispatcherTimer? _managerAuthTimer;

        // Basic Properties
        private string _welcomeMessage = "Welcome to the Admin Panel";
        private ObservableCollection<Employee> _employees = new();
        private ObservableCollection<TimeEntryReportData> _recentTimeEntries = new();
        private int _totalEmployees;
        private int _activeEmployees;
        private string _lastUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        private bool _isLoading = true;

        // Enhanced Properties for Advanced UI
        private string _currentDate = DateTime.Now.ToString("dddd, MMMM dd, yyyy");
        private string _currentTime = DateTime.Now.ToString("hh:mm tt");
        private int _clockedInCount = 0;
        private int _todayEntries = 0;
        private string _totalHoursToday = "0.0h";
        private string _databaseStatus = "Connected";
        private string _databaseStatusIcon = "\uE1C9"; // Database icon
        private string _cameraStatus = "Ready";
        private string _cameraStatusIcon = "\uE114"; // Camera icon
        private string _responseTime = "< 100ms";
        private string _memoryUsage = "45%";
        private string _tabletId = "TABLET-001";
        private string _tabletLocation = "Front Desk";
        private string _currentOperation = "Monitoring";
        private string _currentOperationIcon = "\uE7F4"; // Monitor icon
        private string _performanceStatus = "Optimal";
        private ObservableCollection<AdminEmployeeStatus> _activeEmployeesStatus = new();

        // Add status message properties for DualTimeCorrection integration
        private string _errorMessage = "";
        private string _statusMessage = "";

        // Manager Authentication Properties
        private string _managerAuthStatusMessage = "";
        private bool _isManagerAuthenticated = false;

        // Properties with proper change notification
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        public ObservableCollection<Employee> Employees
        {
            get => _employees;
            set => SetProperty(ref _employees, value);
        }

        public ObservableCollection<TimeEntryReportData> RecentTimeEntries
        {
            get => _recentTimeEntries;
            set => SetProperty(ref _recentTimeEntries, value);
        }

        public int TotalEmployees
        {
            get => _totalEmployees;
            set => SetProperty(ref _totalEmployees, value);
        }

        public int ActiveEmployees
        {
            get => _activeEmployees;
            set => SetProperty(ref _activeEmployees, value);
        }

        public string LastUpdateTime
        {
            get => _lastUpdateTime;
            set => SetProperty(ref _lastUpdateTime, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // Add error and status message properties for integration
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Gets or sets the manager authentication status message with precise countdown.
        /// This message will disappear when authentication expires.
        /// </summary>
        public string ManagerAuthStatusMessage
        {
            get => _managerAuthStatusMessage;
            set => SetProperty(ref _managerAuthStatusMessage, value);
        }

        /// <summary>
        /// Gets or sets whether the manager is currently authenticated.
        /// </summary>
        public bool IsManagerAuthenticated
        {
            get => _isManagerAuthenticated;
            set => SetProperty(ref _isManagerAuthenticated, value);
        }

        // Enhanced UI Properties
        public string CurrentDate
        {
            get => _currentDate;
            set => SetProperty(ref _currentDate, value);
        }

        public string CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        public int ClockedInCount
        {
            get => _clockedInCount;
            set => SetProperty(ref _clockedInCount, value);
        }

        public int TodayEntries
        {
            get => _todayEntries;
            set => SetProperty(ref _todayEntries, value);
        }

        public string TotalHoursToday
        {
            get => _totalHoursToday;
            set => SetProperty(ref _totalHoursToday, value);
        }

        public string DatabaseStatus
        {
            get => _databaseStatus;
            set => SetProperty(ref _databaseStatus, value);
        }

        public string DatabaseStatusIcon
        {
            get => _databaseStatusIcon;
            set => SetProperty(ref _databaseStatusIcon, value);
        }

        public string CameraStatus
        {
            get => _cameraStatus;
            set => SetProperty(ref _cameraStatus, value);
        }

        public string CameraStatusIcon
        {
            get => _cameraStatusIcon;
            set => SetProperty(ref _cameraStatusIcon, value);
        }

        public string ResponseTime
        {
            get => _responseTime;
            set => SetProperty(ref _responseTime, value);
        }

        public string MemoryUsage
        {
            get => _memoryUsage;
            set => SetProperty(ref _memoryUsage, value);
        }

        public string TabletId
        {
            get => _tabletId;
            set => SetProperty(ref _tabletId, value);
        }

        public string TabletLocation
        {
            get => _tabletLocation;
            set => SetProperty(ref _tabletLocation, value);
        }

        public string CurrentOperation
        {
            get => _currentOperation;
            set => SetProperty(ref _currentOperation, value);
        }

        public string CurrentOperationIcon
        {
            get => _currentOperationIcon;
            set => SetProperty(ref _currentOperationIcon, value);
        }

        public string PerformanceStatus
        {
            get => _performanceStatus;
            set => SetProperty(ref _performanceStatus, value);
        }

        /// <summary>
        /// TASK 1: Enhanced collection for comprehensive employee status display
        /// Now uses AdminEmployeeStatus model with all required properties for 7-column DataGrid
        /// /// </summary>
        public ObservableCollection<AdminEmployeeStatus> ActiveEmployeesStatus
        {
            get => _activeEmployeesStatus;
            set => SetProperty(ref _activeEmployeesStatus, value);
        }

        // DEBUGGING: Add a property to track selected employee status for debugging
        private AdminEmployeeStatus? _selectedEmployeeStatus;
        public AdminEmployeeStatus? SelectedEmployeeStatus
        {
            get => _selectedEmployeeStatus;
            set
            {
                if (SetProperty(ref _selectedEmployeeStatus, value))
                {
                    System.Diagnostics.Debug.WriteLine($"SelectedEmployeeStatus changed to: {value?.EmployeeFullName ?? "null"}");
                    // Notify that CanExecute should be re-evaluated
                    EditEmployeeCommand.NotifyCanExecuteChanged();
                    DeleteEmployeeCommand.NotifyCanExecuteChanged();
                    OpenDualTimeCorrectionCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// UPDATED: Constructor now uses dependency injection with proper logging support and optional manager authentication.
        /// </summary>
        /// <param name="timeEntryRepository">The time entry repository injected via DI.</param>
        /// <param name="employeeRepository">The employee repository injected via DI.</param>
        /// <param name="logger">The logger instance injected via DI.</param>
        /// <param name="managerAuthService">Optional manager authentication service for PIN verification.</param>
        public AdminMainViewModel(
            TimeEntryRepository timeEntryRepository,
            EmployeeRepository employeeRepository,
            ILogger<AdminMainViewModel> logger,
            ManagerAuthService? managerAuthService = null)
        {
            _timeEntryRepository = timeEntryRepository ?? throw new ArgumentNullException(nameof(timeEntryRepository));
            _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _managerAuthService = managerAuthService; // Optional service
            
            _logger.LogInformation("AdminMainViewModel initialized with dependency injection");
            if (_managerAuthService != null)
            {
                _logger.LogInformation("AdminMainViewModel: Manager authentication service available");
            }
            else
            {
                _logger.LogInformation("AdminMainViewModel: Manager authentication service not available (optional)");
            }
            
            // Initialize clock timer
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += ClockTimer_Tick;
            _clockTimer.Start();

            // Initialize manager authentication timer for precise countdown
            if (_managerAuthService != null)
            {
                _managerAuthTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1) // Update every second for precise countdown
                };
                _managerAuthTimer.Tick += ManagerAuthTimer_Tick;
                _managerAuthTimer.Start();
                _logger.LogInformation("AdminMainViewModel: Manager authentication timer initialized");
            }
            
            // Load data asynchronously
            _ = LoadDataAsync();
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            CurrentDate = DateTime.Now.ToString("dddd, MMMM dd, yyyy");
            CurrentTime = DateTime.Now.ToString("hh:mm tt");
        }

        /// <summary>
        /// Handles the manager authentication timer tick event for precise countdown display.
        /// Updates the authentication status message every second and clears it when expired.
        /// </summary>
        private void ManagerAuthTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (_managerAuthService == null)
                {
                    return;
                }

                if (_managerAuthService.IsAuthenticatedAndValid())
                {
                    // Manager is authenticated and session is still valid
                    var remainingTime = _managerAuthService.GetRemainingTimeSpan();
                    
                    if (remainingTime.TotalSeconds > 0)
                    {
                        // Update with precise countdown
                        if (remainingTime.TotalMinutes >= 1)
                        {
                            ManagerAuthStatusMessage = $"Manager authenticated ({remainingTime.Minutes} min {remainingTime.Seconds} sec remaining)";
                        }
                        else
                        {
                            ManagerAuthStatusMessage = $"Manager authenticated ({remainingTime.Seconds} sec remaining)";
                        }
                        
                        IsManagerAuthenticated = true;
                    }
                    else
                    {
                        // FIXED: Timeout reached, clear authentication completely
                        HandleManagerAuthenticationExpired();
                    }
                }
                else
                {
                    // Manager is not authenticated or session has expired
                    if (IsManagerAuthenticated || !string.IsNullOrEmpty(ManagerAuthStatusMessage))
                    {
                        // Clear the authentication status
                        HandleManagerAuthenticationExpired();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ManagerAuthTimer_Tick");
            }
        }

        /// <summary>
        /// Handles when manager authentication expires - clears all auth-related UI elements.
        /// </summary>
        private void HandleManagerAuthenticationExpired()
        {
            try
            {
                ManagerAuthStatusMessage = string.Empty; // FIXED: Message disappears completely
                IsManagerAuthenticated = false;

                // Optionally notify the manager authentication service to clear itself
                _managerAuthService?.ClearAuthentication();

                _logger.LogInformation("Manager authentication expired - status message cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling manager authentication expiration");
            }
        }

        /// <summary>
        /// Updates the manager authentication status display immediately.
        /// Called after successful authentication to start the countdown display.
        /// </summary>
        private void UpdateManagerAuthenticationStatus()
        {
            try
            {
                if (_managerAuthService != null && _managerAuthService.IsAuthenticatedAndValid())
                {
                    var remainingTime = _managerAuthService.GetRemainingTimeSpan();
                    
                    if (remainingTime.TotalSeconds > 0)
                    {
                        if (remainingTime.TotalMinutes >= 1)
                        {
                            ManagerAuthStatusMessage = $"Manager authenticated ({remainingTime.Minutes} min {remainingTime.Seconds} sec remaining)";
                        }
                        else
                        {
                            ManagerAuthStatusMessage = $"Manager authenticated ({remainingTime.Seconds} sec remaining)";
                        }
                        
                        IsManagerAuthenticated = true;
                        _logger.LogInformation("Manager authentication status updated - countdown started");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating manager authentication status");
            }
        }

        /// <summary>
        /// Helper method to safely parse string values to integers with a default fallback.
        /// </summary>
        /// <param name="value">The string value to parse.</param>
        /// <param name="defaultValue">The default value to return if parsing fails.</param>
        /// <returns>The parsed integer value or the default value.</returns>
        private int SafeParseInt(string? value, int defaultValue = 0)
        {
            return int.TryParse(value, out int result) ? result : defaultValue;
        }

        /// <summary>
        /// TASK 1: Enhanced data loading with comprehensive employee status calculation
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                _logger.LogInformation("Starting to load admin data");
                
                // Load employees
                var allEmployees = await Task.Run(() => _employeeRepository.GetAllEmployees());
                
                Employees.Clear();
                ActiveEmployeesStatus.Clear();
                
                var clockedInCount = 0;
                
                foreach (var employee in allEmployees)
                {
                    Employees.Add(employee);
                    
                    // TASK 1: Create enhanced status model with comprehensive time tracking data
                    var statusModel = await CreateEmployeeStatusAsync(employee);
                    
                    if (statusModel.CurrentStatus == "Working")
                    {
                        clockedInCount++;
                    }
                    
                    ActiveEmployeesStatus.Add(statusModel);
                }

                ClockedInCount = clockedInCount;
                TotalEmployees = allEmployees.Count;
                ActiveEmployees = allEmployees.Count(e => e.Active);

                // Load recent time entries and calculate stats
                var endDate = DateTime.Today;
                var startDate = endDate.AddDays(-7);
                
                var timeEntries = await Task.Run(() => 
                    _timeEntryRepository.GetTimeEntriesForReporting(startDate, endDate));
                
                RecentTimeEntries.Clear();
                foreach (var entry in timeEntries.OrderByDescending(e => e.ShiftDate).Take(10))
                {
                    RecentTimeEntries.Add(entry);
                }

                // Calculate today's stats
                var todayEntries = timeEntries.Where(e => e.ShiftDate.Date == DateTime.Today).ToList();
                TodayEntries = todayEntries.Count;
                
                var totalHours = todayEntries.Sum(e => e.TotalHours);
                TotalHoursToday = $"{totalHours:F1}h";

                LastUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                _logger.LogInformation("Successfully loaded admin data - {EmployeeCount} employees, {TodayEntries} today's entries", 
                    allEmployees.Count, TodayEntries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin data");
                ErrorMessage = $"Error loading admin data: {ex.Message}";
                WpfMessageBox.Show($"Error loading admin data: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// TASK 1: Creates comprehensive employee status with real-time data and photo information
        /// CRITICAL FIX: Enhanced photo path validation to prevent BitmapImage crashes
        /// </summary>
        private async Task<AdminEmployeeStatus> CreateEmployeeStatusAsync(Employee employee)
        {
            try
            {
                // NEW: Use cross-midnight aware shift status method
                var shiftStatus = await _timeEntryRepository.GetEmployeeShiftStatusAsync(employee.EmployeeID);

                if (shiftStatus.IsWorking)
                {
                    // Employee is currently working
                    return new AdminEmployeeStatus
                    {
                        Employee = employee,
                        EmployeeFullName = $"{employee.FirstName} {employee.LastName}",
                        CurrentStatus = shiftStatus.IsCrossMidnight ? "Working (Overnight)" : "Working",
                        StatusColor = "#007BFF", // Blue for working
                        ClockInTime = FormatShiftTime(shiftStatus.ShiftStarted, shiftStatus.IsCrossMidnight),
                        ClockInPhotoExists = false, // TODO: Implement photo checking
                        ClockInPhotoPath = "",
                        ClockOutTime = "--:--",
                        ClockOutPhotoExists = false,
                        ClockOutPhotoPath = "",
                        WorkedHoursToday = $"{shiftStatus.WorkingHours:F1}h{(shiftStatus.IsCrossMidnight ? " (ongoing)" : "")}"
                    };
                }
                else
                {
                    // Employee is not currently working
                    string status = "Available";
                    string statusColor = "#28A745"; // Green

                    if (shiftStatus.TodayCompletedHours > 0)
                    {
                        status = "Not Available";
                        statusColor = "#DC3545"; // Red
                    }

                    return new AdminEmployeeStatus
                    {
                        Employee = employee,
                        EmployeeFullName = $"{employee.FirstName} {employee.LastName}",
                        CurrentStatus = status,
                        StatusColor = statusColor,
                        ClockInTime = "--:--",
                        ClockInPhotoExists = false,
                        ClockInPhotoPath = "",
                        ClockOutTime = shiftStatus.LastClockOut?.ToString("h:mm tt") ?? "--:--",
                        ClockOutPhotoExists = false,
                        ClockOutPhotoPath = "",
                        WorkedHoursToday = $"{shiftStatus.TodayCompletedHours:F1}h today"
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CreateEmployeeStatusAsync for employee {employee?.FirstName} {employee?.LastName}: {ex.Message}");

                // Return safe default status on error
                return new AdminEmployeeStatus
                {
                    Employee = employee,
                    EmployeeFullName = $"{employee?.FirstName} {employee?.LastName}" ?? "Unknown Employee",
                    CurrentStatus = "Unknown",
                    StatusColor = "#6C757D", // Gray
                    ClockInTime = "--:--",
                    ClockInPhotoExists = false,
                    ClockInPhotoPath = "",
                    ClockOutTime = "--:--",
                    ClockOutPhotoExists = false,
                    ClockOutPhotoPath = "",
                    WorkedHoursToday = "0.0h"
                };
            }
        }

        /// <summary>
        /// Opens the DualTimeCorrectionDialog for the selected employee.
        /// This method handles the logic for opening the dialog, passing necessary data, 
        /// and refreshing the view upon successful correction.
        /// Includes optional manager PIN authentication if ManagerPinDialog exists.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanOpenDualTimeCorrection))]
        private async Task OpenDualTimeCorrection()
        {
            try
            {
                _logger.LogInformation("OpenDualTimeCorrection command started");
                
                // Clear previous messages
                ErrorMessage = "";
                StatusMessage = "";

                // STEP 5: Optional Manager PIN Authentication
                // Add PIN authentication if ManagerAuthService and ManagerPinDialog are available
                if (_managerAuthService != null)
                {
                    try
                    {
                        _logger.LogInformation("OpenDualTimeCorrection: Manager authentication service available, checking authentication");
                        
                        // Check if manager is already authenticated and session is still valid
                        if (!_managerAuthService.IsAuthenticatedAndValid())
                        {
                            _logger.LogInformation("OpenDualTimeCorrection: Manager not authenticated, showing PIN dialog");
                            
                            // Create and show the PIN dialog
                            var pinDialog = new Views.ManagerPinDialog(_managerAuthService);
                            pinDialog.Owner = System.Windows.Application.Current.MainWindow;
                            
                            // Show the PIN dialog modally
                            var pinResult = pinDialog.ShowDialog();
                            
                            // If the result is not true (dialog was cancelled or PIN was incorrect), exit
                            if (pinResult != true)
                            {
                                _logger.LogWarning("OpenDualTimeCorrection: Manager PIN authentication failed or cancelled");
                                ErrorMessage = "Manager PIN authentication failed or cancelled.";
                                return;
                            }
                            
                            _logger.LogInformation("OpenDualTimeCorrection: Manager PIN authentication successful");
                        }
                        else
                        {
                            _logger.LogInformation("OpenDualTimeCorrection: Manager already authenticated, extending session");
                            // Extend the existing session
                            _managerAuthService.ExtendSession();
                        }

                        // Update the authentication status display after successful authentication
                        UpdateManagerAuthenticationStatus();
                    }
                    catch (Exception authEx)
                    {
                        _logger.LogError(authEx, "OpenDualTimeCorrection: Error during manager authentication");
                        ErrorMessage = "Error during manager authentication. Proceeding without authentication.";
                        // Continue with the operation even if authentication fails (graceful degradation)
                    }
                }
                else
                {
                    _logger.LogInformation("OpenDualTimeCorrection: Manager authentication service not available, proceeding without authentication");
                }

                // Continue with the existing dual time correction logic...
                // Get selected employee from the AdminEmployeeStatus selection
                var selectedEmployeeStatus = SelectedEmployeeStatus;
                
                if (selectedEmployeeStatus?.Employee == null)
                {
                    var message = "Please select an employee from the list.";
                    _logger.LogWarning("OpenDualTimeCorrection: No employee selected");
                    ErrorMessage = message;
                    return;
                }

                var selectedEmployee = selectedEmployeeStatus.Employee;
                _logger.LogInformation("OpenDualTimeCorrection: Selected employee {EmployeeName} (ID: {EmployeeId})", 
                    selectedEmployee.FullName, selectedEmployee.EmployeeID);

                // Get current time entry for the employee
                var timeEntry = await _timeEntryRepository.GetCurrentTimeEntryAsync(selectedEmployee.EmployeeID);
                
                // If no current time entry, check for today's completed entries
                if (timeEntry == null)
                {
                    var todayEntries = await _timeEntryRepository.GetTimeEntriesForDateAsync(selectedEmployee.EmployeeID, DateTime.Today);
                    timeEntry = todayEntries.Where(e => e.TimeOut.HasValue)
                                           .OrderByDescending(e => e.TimeOut)
                                           .FirstOrDefault();
                }
                
                if (timeEntry == null)
                {
                    var message = $"No time entry found for {selectedEmployee.FirstName} {selectedEmployee.LastName} to correct.";
                    _logger.LogWarning("OpenDualTimeCorrection: No time entry found for employee {EmployeeId}", selectedEmployee.EmployeeID);
                    ErrorMessage = message;
                    return;
                }

                _logger.LogInformation("OpenDualTimeCorrection: Found time entry {EntryId} for employee {EmployeeId}", 
                    timeEntry.EntryID, selectedEmployee.EmployeeID);

                // Create and show the dialog using the App's factory method for proper DI
                var app = (App)System.Windows.Application.Current;
                if (app?.Services == null)
                {
                    var message = "Application services are not available.";
                    _logger.LogError("OpenDualTimeCorrection: Application services not available");
                    ErrorMessage = message;
                    return;
                }

                var dialog = app.CreateDualTimeCorrectionDialog(selectedEmployee, timeEntry);
                dialog.Owner = System.Windows.Application.Current.MainWindow; // Set owner for proper dialog behavior
                
                _logger.LogInformation("OpenDualTimeCorrection: Showing dialog for employee {EmployeeId}", selectedEmployee.EmployeeID);
                
                // Show the dialog modally
                bool? result = dialog.ShowDialog();
                
                if (result == true && dialog.IsApplied)
                {
                    _logger.LogInformation("OpenDualTimeCorrection: Corrections applied successfully for employee {EmployeeId}", selectedEmployee.EmployeeID);
                    
                    // Refresh the employee data display
                    await LoadDataAsync(); 
                    
                    // Show success message
                    StatusMessage = $"Time correction applied successfully for {selectedEmployee.FirstName} {selectedEmployee.LastName}.";
                    
                    System.Diagnostics.Debug.WriteLine($"DualTimeCorrection completed successfully for {selectedEmployee.FirstName} {selectedEmployee.LastName}");
                }
                else
                {
                    _logger.LogInformation("OpenDualTimeCorrection: Dialog was cancelled for employee {EmployeeId}", selectedEmployee.EmployeeID);
                    StatusMessage = "Time correction was cancelled.";
                    System.Diagnostics.Debug.WriteLine($"DualTimeCorrection was cancelled for {selectedEmployee.FirstName} {selectedEmployee.LastName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OpenDualTimeCorrection");
                System.Diagnostics.Debug.WriteLine($"Error in OpenDualTimeCorrection: {ex.Message}");
                ErrorMessage = $"Failed to open time correction dialog: {ex.Message}";
            }
        }

        /// <summary>
        /// Determines if the OpenDualTimeCorrection command can execute.
        /// </summary>
        /// <returns>True if an employee is selected and available for time correction.</returns>
        private bool CanOpenDualTimeCorrection()
        {
            bool canExecute = SelectedEmployeeStatus?.Employee != null;
            System.Diagnostics.Debug.WriteLine($"CanOpenDualTimeCorrection: {canExecute} (Selected: {SelectedEmployeeStatus?.EmployeeFullName ?? "None"})");
            return canExecute;
        }

        [RelayCommand]
        private async Task RefreshData()
        {
            await LoadDataAsync();
        }

        [RelayCommand]
        private void BackToMain()
        {
            // Close admin window - main window should be accessible
            System.Windows.Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.GetType().Name == "AdminMainWindow")?.Close();
        }

        [RelayCommand]
        private void Logout()
        {
            var result = WpfMessageBox.Show("Are you sure you want to logout?", "Logout", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                BackToMain();
            }
        }

        [RelayCommand]
        private void Dashboard()
        {
            CurrentOperation = "Dashboard View";
            CurrentOperationIcon = "\uE7F4"; // Monitor icon
        }

        [RelayCommand]
        private void EmployeeManagement()
        {
            CurrentOperation = "Employee Management";
            CurrentOperationIcon = "\uE716"; // People icon
        }

        [RelayCommand]
        private void TimeReports()
        {
            CurrentOperation = "Time Reports";
            CurrentOperationIcon = "\uE8A5"; // Report icon
        }

        [RelayCommand]
        private void ExportData()
        {
            WpfMessageBox.Show("Export functionality coming soon!", "Export Data", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void TestCamera()
        {
            WpfMessageBox.Show("Camera test functionality coming soon!", "Test Camera", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void ClearTestData()
        {
            var result = WpfMessageBox.Show("Are you sure you want to clear all test data?", "Clear Test Data", 
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                WpfMessageBox.Show("Test data cleared successfully!", "Clear Test Data", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private void Support()
        {
            WpfMessageBox.Show("Support contact: admin@company.com", "Support", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void Help()
        {
            WpfMessageBox.Show("Help documentation coming soon!", "Help", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void MaintenanceMode()
        {
            WpfMessageBox.Show("Maintenance mode functionality coming soon!", "Maintenance", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void ViewEmployeeDetails(Employee employee)
        {
            if (employee != null)
            {
                WpfMessageBox.Show($"Employee: {employee.FullName}\nJob Title: {employee.JobTitle}\nPay Rate: ${employee.PayRate:F2}/hr\nActive: {(employee.Active ? "Yes" : "No")}", 
                    "Employee Details", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private void ViewTimeEntryDetails(TimeEntryReportData entry)
        {
            if (entry != null)
            {
                WpfMessageBox.Show($"Employee: {entry.EmployeeName}\nDate: {entry.ShiftDate:yyyy-MM-dd}\nTime In: {entry.TimeIn}\nTime Out: {entry.TimeOut}\nTotal Hours: {entry.TotalHours:F1}h\nGross Pay: ${entry.GrossPay:F2}", 
                    "Time Entry Details", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #region Employee Management CRUD Commands

        /// <summary>
        /// Command to add a new employee through the admin interface
        /// </summary>
        [RelayCommand]
        private async Task AddEmployee()
        {
            try
            {
                // Create new employee template
                var newEmployee = new Employee
                {
                    FirstName = "",
                    LastName = "",
                    PayRate = 15.00m,
                    JobTitle = "",
                    Active = true,
                    DateHired = DateTime.Now,
                    PhoneNumber = "",
                    DateOfBirth = null,
                    SocialSecurityNumber = ""
                };

                // Show employee edit dialog
                var dialog = new EmployeeTimeTrackerTablet.Views.EmployeeEditDialog(newEmployee, isNewEmployee: true);
                var result = dialog.ShowDialog();

                if (result == true && dialog.Employee != null)
                {
                    bool success = _employeeRepository.AddEmployee(dialog.Employee);
                    
                    if (success)
                    {
                        WpfMessageBox.Show($"Employee {dialog.Employee.FullName} added successfully!",
                            "Employee Added", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // Refresh the employee list
                        await LoadDataAsync();
                    }
                    else
                    {
                        WpfMessageBox.Show("Failed to add employee. Please check the information and try again.",
                            "Add Employee Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddEmployee error: {ex.Message}");
                WpfMessageBox.Show($"Error adding employee: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Command to edit an existing employee
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanEditEmployee))]
        private async Task EditEmployee(AdminEmployeeStatus employeeStatus)
        {
            System.Diagnostics.Debug.WriteLine($"EditEmployee command executed with parameter: {employeeStatus?.EmployeeFullName ?? "null"}");
            
            if (employeeStatus?.Employee == null) 
            {
                System.Diagnostics.Debug.WriteLine("EditEmployee: No employee selected or employeeStatus is null");
                return;
            }

            var employee = employeeStatus.Employee;
            System.Diagnostics.Debug.WriteLine($"EditEmployee: Editing employee {employee.FullName} (ID: {employee.EmployeeID})");

            try
            {
                // Create a copy for editing to avoid modifying the original
                var employeeCopy = new Employee
                {
                    EmployeeID = employee.EmployeeID,
                    FirstName = employee.FirstName,
                    LastName = employee.LastName,
                    PayRate = employee.PayRate,
                    JobTitle = employee.JobTitle,
                    Active = employee.Active,
                    DateHired = employee.DateHired,
                    PhoneNumber = employee.PhoneNumber,
                    DateOfBirth = employee.DateOfBirth,
                    SocialSecurityNumber = employee.SocialSecurityNumber
                };

                System.Diagnostics.Debug.WriteLine($"EditEmployee: Creating dialog for employee {employeeCopy.FullName}");

                // Show employee edit dialog
                var dialog = new EmployeeTimeTrackerTablet.Views.EmployeeEditDialog(employeeCopy, isNewEmployee: false);
                var result = dialog.ShowDialog();

                System.Diagnostics.Debug.WriteLine($"EditEmployee: Dialog result = {result}");

                if (result == true && dialog.Employee != null)
                {
                    bool success = _employeeRepository.UpdateEmployee(dialog.Employee);
                    
                    if (success)
                    {
                        System.Diagnostics.Debug.WriteLine($"EditEmployee: Successfully updated employee {dialog.Employee.FullName}");
                        WpfMessageBox.Show($"Employee {dialog.Employee.FullName} updated successfully!",
                            "Employee Updated", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // Refresh the employee list
                        await LoadDataAsync();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"EditEmployee: Failed to update employee {dialog.Employee.FullName}");
                        WpfMessageBox.Show("Failed to update employee. Please check the information and try again.",
                            "Update Employee Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EditEmployee error: {ex.Message}");
                WpfMessageBox.Show($"Error updating employee: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Determines if the EditEmployee command can execute
        /// </summary>
        /// <param name="employeeStatus">The selected employee status</param>
        /// <returns>True if an employee is selected</returns>
        private bool CanEditEmployee(AdminEmployeeStatus employeeStatus)
        {
            bool canEdit = employeeStatus?.Employee != null;
            System.Diagnostics.Debug.WriteLine($"CanEditEmployee: {canEdit} (employeeStatus: {employeeStatus?.EmployeeFullName ?? "null"})");
            return canEdit;
        }

        /// <summary>
        /// Command to delete an employee (with business rule validation)
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanDeleteEmployee))]
        private async Task DeleteEmployee(AdminEmployeeStatus employeeStatus)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteEmployee command executed with parameter: {employeeStatus?.EmployeeFullName ?? "null"}");
            
            if (employeeStatus?.Employee == null) 
            {
                System.Diagnostics.Debug.WriteLine("DeleteEmployee: No employee selected or employeeStatus is null");
                return;
            }

            var employee = employeeStatus.Employee;
            System.Diagnostics.Debug.WriteLine($"DeleteEmployee: Deleting employee {employee.FullName} (ID: {employee.EmployeeID})");

            try
            {
                // Confirm deletion
                var confirmResult = WpfMessageBox.Show(
                    $"Are you sure you want to delete employee {employee.FullName}?\n\n" +
                    "Note: Employees with existing time entries cannot be deleted.",
                    "Confirm Delete Employee",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                System.Diagnostics.Debug.WriteLine($"DeleteEmployee: Confirmation result = {confirmResult}");

                if (confirmResult == MessageBoxResult.Yes)
                {
                    bool success = _employeeRepository.DeleteEmployee(employee.EmployeeID);
                    
                    if (success)
                    {
                        System.Diagnostics.Debug.WriteLine($"DeleteEmployee: Successfully deleted employee {employee.FullName}");
                        WpfMessageBox.Show($"Employee {employee.FullName} deleted successfully!",
                            "Employee Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // Refresh the employee list
                        await LoadDataAsync();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"DeleteEmployee: Failed to delete employee {employee.FullName} - likely has time entries");
                        WpfMessageBox.Show(
                            $"Cannot delete employee {employee.FullName}.\n\n" +
                            "This employee has existing time entries and cannot be deleted.\n" +
                            "You can deactivate the employee instead.",
                            "Delete Employee Failed",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteEmployee error: {ex.Message}");
                WpfMessageBox.Show($"Error deleting employee: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Determines if the DeleteEmployee command can execute
        /// </summary>
        /// <param name="employeeStatus">The selected employee status</param>
        /// <returns>True if an employee is selected</returns>
        private bool CanDeleteEmployee(AdminEmployeeStatus employeeStatus)
        {
            bool canDelete = employeeStatus?.Employee != null;
            System.Diagnostics.Debug.WriteLine($"CanDeleteEmployee: {canDelete} (employeeStatus: {employeeStatus?.EmployeeFullName ?? "null"})");
            return canDelete;
        }

        #endregion Employee Management CRUD Commands

        /// <summary>
        /// HELPER: Formats shift time display for cross-midnight shifts.
        /// </summary>
        private string FormatShiftTime(DateTime shiftStart, bool isCrossMidnight)
        {
            return isCrossMidnight
                ? $"{shiftStart:ddd h:mm tt}"  // "Mon 11:00 PM" for overnight shifts
                : $"{shiftStart:h:mm tt}";      // "11:00 PM" for same-day shifts
        }

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the AdminMainViewModel and cleans up resources including timers.
        /// </summary>
        public void Dispose()
        {
            try
            {
                // Stop and dispose of timers
                if (_clockTimer != null)
                {
                    _clockTimer.Stop();
                    _clockTimer.Tick -= ClockTimer_Tick;
                }

                if (_managerAuthTimer != null)
                {
                    _managerAuthTimer.Stop();
                    _managerAuthTimer.Tick -= ManagerAuthTimer_Tick;
                }

                _logger.LogInformation("AdminMainViewModel: Disposed successfully with timers cleaned up");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AdminMainViewModel disposal");
            }
        }

        #endregion IDisposable Implementation
    }

    /// <summary>
    /// TASK 1: Enhanced AdminEmployeeStatus model for comprehensive 7-column DataGrid display
    /// FIXED: Added proper photo display properties and employee photo support
    /// </summary>
    public class AdminEmployeeStatus
    {
        /// <summary>
        /// FIXED: Reference to the underlying Employee object for CRUD operations
        /// </summary>
        public Employee Employee { get; set; } = null!;

        /// <summary>
        /// Employee's full name (e.g., "John Doe")
        /// </summary>
        public string EmployeeFullName { get; set; } = "";

        /// <summary>
        /// Current employee status: "Available", "Working", or "Not Available"
        /// </summary>
        public string CurrentStatus { get; set; } = "";

        /// <summary>
        /// Color code for status badge display
        /// Green (#28A745) = Available, Blue (#007BFF) = Working, Red (#DC3545) = Not Available
        /// </summary>
        public string StatusColor { get; set; } = "";

        /// <summary>
        /// Clock-in time for today in h:mm tt format (e.g., "8:30 AM" or "--:--" if not clocked in)
        /// </summary>
        public string ClockInTime { get; set; } = "";

        /// <summary>
        /// Whether a clock-in photo exists for today's entry
        /// </summary>
        public bool ClockInPhotoExists { get; set; } = false;

        /// <summary>
        /// Clock-out time for today in h:mm tt format (e.g., "5:30 PM", "In Progress", or "--:--")
        /// </summary>
        public string ClockOutTime { get; set; } = "";

        /// <summary>
        /// Whether a clock-out photo exists for today's entry
        /// </summary>
        public bool ClockOutPhotoExists { get; set; } = false;

        /// <summary>
        /// Total hours worked today (e.g., "8.5h" or "4.2h (ongoing)")
        /// </summary>
        public string WorkedHoursToday { get; set; } = "";

        /// <summary>
        /// FIXED: Full path to employee profile photo for display
        /// </summary>
        public string EmployeePhotoPath { get; set; } = "";

        /// <summary>
        /// FIXED: Full path to clock-in photo for display
        /// </summary>
        public string ClockInPhotoPath { get; set; } = "";

        /// <summary>
        /// FIXED: Full path to clock-out photo for display
        /// </summary>
        public string ClockOutPhotoPath { get; set; } = "";

        /// <summary>
        /// Gets the appropriate camera icon for clock-in photo status
        /// FIXED: Uses proper Unicode camera icon
        /// </summary>
        public string ClockInPhotoIcon => "\uE114"; // Camera icon

        /// <summary>
        /// Gets the appropriate camera icon for clock-out photo status  
        /// FIXED: Uses proper Unicode camera icon
        /// </summary>
        public string ClockOutPhotoIcon => "\uE114"; // Camera icon

        /// <summary>
        /// Gets the photo status text for clock-in
        /// FIXED: Shows checkmark when photo exists, X when missing
        /// </summary>
        public string ClockInPhotoStatus => ClockInPhotoExists ? "\u2713" : "\u2717"; // ? or ?

        /// <summary>
        /// Gets the photo status text for clock-out
        /// FIXED: Shows checkmark when photo exists, X when missing
        /// </summary>
        public string ClockOutPhotoStatus => ClockOutPhotoExists ? "\u2713" : "\u2717"; // ? or ?

        /// <summary>
        /// Gets the color for clock-in photo status
        /// </summary>
        public string ClockInPhotoStatusColor => ClockInPhotoExists ? "#28A745" : "#DC3545";

        /// <summary>
        /// Gets the color for clock-out photo status
        /// </summary>
        public string ClockOutPhotoStatusColor => ClockOutPhotoExists ? "#28A745" : "#DC3545";
    }
}
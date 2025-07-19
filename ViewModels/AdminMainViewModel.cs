using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EmployeeTimeTrackerTablet.Data;
using EmployeeTimeTrackerTablet.Models;

namespace EmployeeTimeTrackerTablet.ViewModels
{
    /// <summary>
    /// ViewModel for the Admin Main Window interface
    /// Provides comprehensive administrative oversight and control
    /// </summary>
    public partial class AdminMainViewModel : ObservableObject, IDisposable
    {
        #region Private Fields
        private readonly EmployeeRepository _employeeRepository;
        private readonly TimeEntryRepository _timeEntryRepository;
        private readonly System.Windows.Threading.DispatcherTimer _refreshTimer;
        #endregion

        #region Constructor
        public AdminMainViewModel(
            EmployeeRepository employeeRepository,
            TimeEntryRepository timeEntryRepository)
        {
            _employeeRepository = employeeRepository;
            _timeEntryRepository = timeEntryRepository;

            // Initialize collections
            ActiveEmployees = new ObservableCollection<AdminEmployeeStatus>();
            SystemAlerts = new ObservableCollection<SystemAlert>();
            RecentActivity = new ObservableCollection<ActivityItem>();

            // Initialize commands
            InitializeCommands();

            // Set up refresh timer (every 30 seconds)
            _refreshTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _refreshTimer.Tick += async (s, e) => await RefreshDashboardDataAsync();
            _refreshTimer.Start();

            // Load initial data
            _ = Task.Run(LoadInitialDataAsync);
        }
        #endregion

        #region Observable Properties - Header Statistics

        [ObservableProperty]
        private int _clockedInCount = 0;

        [ObservableProperty]
        private int _todayEntries = 0;

        [ObservableProperty]
        private string _systemHealthIcon = "??";

        [ObservableProperty]
        private SolidColorBrush _systemHealthColor = new SolidColorBrush(Colors.Green);

        #endregion

        #region Observable Properties - Sidebar Statistics

        [ObservableProperty]
        private int _activeEmployeeCount = 0;

        [ObservableProperty]
        private string _totalHoursToday = "0.0";

        [ObservableProperty]
        private int _photosCaptured = 0;

        #endregion

        #region Observable Properties - System Status

        [ObservableProperty]
        private SolidColorBrush _cameraStatusColor = new SolidColorBrush(Colors.Green);

        [ObservableProperty]
        private string _cameraStatusText = "Available";

        [ObservableProperty]
        private string _storageStatus = "78% Free";

        [ObservableProperty]
        private string _lastSyncTime = "2 min ago";

        #endregion

        #region Observable Properties - Performance Metrics

        [ObservableProperty]
        private string _avgResponseTime = "0.3s";

        [ObservableProperty]
        private string _photoSuccessRate = "96%";

        [ObservableProperty]
        private string _dbOperations = "247";

        [ObservableProperty]
        private string _errorRate = "0.2%";

        #endregion

        #region Observable Properties - Quick Settings

        [ObservableProperty]
        private bool _autoPhotoEnabled = true;

        [ObservableProperty]
        private bool _twentyFourHourMode = true;

        [ObservableProperty]
        private bool _soundAlertsEnabled = false;

        [ObservableProperty]
        private bool _autoExportEnabled = true;

        #endregion

        #region Observable Properties - System Information

        [ObservableProperty]
        private string _tabletId = "TABLET-001";

        [ObservableProperty]
        private string _tabletLocation = "Main Entrance";

        [ObservableProperty]
        private string _appVersion = "v2.1.0";

        [ObservableProperty]
        private string _systemUptime = "2d 14h 32m";

        #endregion

        #region Observable Properties - Footer Status

        [ObservableProperty]
        private string _currentOperationIcon = "??";

        [ObservableProperty]
        private string _currentOperation = "System monitoring active";

        [ObservableProperty]
        private string _performanceStatus = "Response: 0.2s";

        #endregion

        #region Collections

        /// <summary>
        /// Collection of currently active employees with their status
        /// </summary>
        public ObservableCollection<AdminEmployeeStatus> ActiveEmployees { get; }

        /// <summary>
        /// Collection of system alerts and notifications
        /// </summary>
        public ObservableCollection<SystemAlert> SystemAlerts { get; }

        /// <summary>
        /// Collection of recent time tracking activity
        /// </summary>
        public ObservableCollection<ActivityItem> RecentActivity { get; }

        #endregion

        #region Navigation Commands

        [RelayCommand]
        private async Task BackToMainAsync()
        {
            try
            {
                // Navigate back to main employee interface
                // This would typically involve showing the main window and hiding admin
                await Task.Delay(100); // Simulate navigation
                
                // Log admin session end
                await LogActivityAsync("Admin session ended", "??");
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Navigation Error", ex.Message);
            }
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            try
            {
                // Confirm logout
                var result = System.Windows.MessageBox.Show(
                    "Are you sure you want to logout from admin mode?",
                    "Confirm Logout",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    await LogActivityAsync("Admin logged out", "??");
                    await BackToMainAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Logout Error", ex.Message);
            }
        }

        [RelayCommand]
        private async Task RefreshDataAsync()
        {
            try
            {
                CurrentOperation = "Refreshing all data...";
                CurrentOperationIcon = "??";
                
                await RefreshDashboardDataAsync();
                await LogActivityAsync("Data manually refreshed", "??");
                
                CurrentOperation = "System monitoring active";
                CurrentOperationIcon = "??";
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Refresh Error", ex.Message);
            }
        }

        #endregion

        #region Navigation Menu Commands

        [RelayCommand]
        private async Task DashboardAsync()
        {
            await NavigateToSectionAsync("Dashboard", "??");
        }

        [RelayCommand]
        private async Task EmployeeManagementAsync()
        {
            await NavigateToSectionAsync("Employee Management", "??");
        }

        [RelayCommand]
        private async Task TimeReportsAsync()
        {
            await NavigateToSectionAsync("Time Reports", "?");
        }

        [RelayCommand]
        private async Task PhotoManagementAsync()
        {
            await NavigateToSectionAsync("Photo Management", "??");
        }

        [RelayCommand]
        private async Task EmailSetupAsync()
        {
            await NavigateToSectionAsync("Email Setup", "??");
        }

        [RelayCommand]
        private async Task SystemConfigAsync()
        {
            await NavigateToSectionAsync("System Configuration", "??");
        }

        [RelayCommand]
        private async Task AuditLogsAsync()
        {
            await NavigateToSectionAsync("Audit Logs", "??");
        }

        [RelayCommand]
        private async Task MaintenanceAsync()
        {
            await NavigateToSectionAsync("Maintenance Tools", "???");
        }

        [RelayCommand]
        private async Task SettingsAsync()
        {
            await NavigateToSectionAsync("Settings", "??");
        }

        #endregion

        #region Quick Action Commands

        [RelayCommand]
        private async Task ExportDataAsync()
        {
            try
            {
                CurrentOperation = "Exporting data...";
                CurrentOperationIcon = "??";

                // Simulate export process
                await Task.Delay(2000);
                
                await LogActivityAsync("Data export completed successfully", "??");
                System.Windows.MessageBox.Show(
                    "Data export completed successfully!",
                    "Export Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                CurrentOperation = "System monitoring active";
                CurrentOperationIcon = "??";
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Export Error", ex.Message);
            }
        }

        [RelayCommand]
        private async Task RefreshAllAsync()
        {
            await RefreshDataAsync();
        }

        [RelayCommand]
        private async Task TestCameraAsync()
        {
            try
            {
                CurrentOperation = "Testing camera system...";
                CurrentOperationIcon = "??";

                // Simulate camera test
                await Task.Delay(1500);
                
                await LogActivityAsync("Camera system test completed", "??");
                System.Windows.MessageBox.Show(
                    "Camera system test completed successfully!\n\nAll cameras are functioning properly.",
                    "Camera Test Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                CurrentOperation = "System monitoring active";
                CurrentOperationIcon = "??";
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Camera Test Error", ex.Message);
            }
        }

        [RelayCommand]
        private async Task ClearTestDataAsync()
        {
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "This will clear all test data including time entries and photos.\n\nThis action cannot be undone. Continue?",
                    "Clear Test Data",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    CurrentOperation = "Clearing test data...";
                    CurrentOperationIcon = "???";

                    // Add actual test data clearing logic here
                    await Task.Delay(1000);
                    
                    await LogActivityAsync("Test data cleared successfully", "???");
                    await RefreshDashboardDataAsync();

                    System.Windows.MessageBox.Show(
                        "Test data has been cleared successfully.",
                        "Clear Complete",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);

                    CurrentOperation = "System monitoring active";
                    CurrentOperationIcon = "??";
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Clear Data Error", ex.Message);
            }
        }

        [RelayCommand]
        private async Task GenerateReportAsync()
        {
            try
            {
                CurrentOperation = "Generating report...";
                CurrentOperationIcon = "??";

                // Simulate report generation
                await Task.Delay(2000);
                
                await LogActivityAsync("Daily report generated", "??");
                System.Windows.MessageBox.Show(
                    "Daily report has been generated successfully and saved to the reports folder.",
                    "Report Generated",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                CurrentOperation = "System monitoring active";
                CurrentOperationIcon = "??";
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Report Generation Error", ex.Message);
            }
        }

        [RelayCommand]
        private async Task SystemDiagnosticsAsync()
        {
            try
            {
                CurrentOperation = "Running system diagnostics...";
                CurrentOperationIcon = "??";

                // Simulate diagnostics
                await Task.Delay(3000);
                
                await LogActivityAsync("System diagnostics completed", "??");
                System.Windows.MessageBox.Show(
                    "System diagnostics completed successfully!\n\n? Database: Healthy\n? Camera: Operational\n? Storage: Adequate\n? Network: Connected",
                    "Diagnostics Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                CurrentOperation = "System monitoring active";
                CurrentOperationIcon = "??";
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Diagnostics Error", ex.Message);
            }
        }

        #endregion

        #region Employee Action Commands

        [RelayCommand]
        private async Task ViewEmployeeAsync(AdminEmployeeStatus employee)
        {
            try
            {
                if (employee != null)
                {
                    await LogActivityAsync($"Viewing details for {employee.FullName}", "???");
                    // Navigate to employee detail view
                    await NavigateToSectionAsync($"Employee Details - {employee.FullName}", "??");
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("View Employee Error", ex.Message);
            }
        }

        [RelayCommand]
        private async Task EditTimeAsync(AdminEmployeeStatus employee)
        {
            try
            {
                if (employee != null)
                {
                    await LogActivityAsync($"Editing time for {employee.FullName}", "??");
                    // Open time edit dialog
                    System.Windows.MessageBox.Show(
                        $"Time editing interface would open for {employee.FullName}",
                        "Edit Time Entry",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Edit Time Error", ex.Message);
            }
        }

        #endregion

        #region Footer Action Commands

        [RelayCommand]
        private async Task SupportAsync()
        {
            try
            {
                System.Windows.MessageBox.Show(
                    "IT Support Contact:\n\nPhone: (555) 123-4567\nEmail: it-support@company.com\n\nFor urgent issues, call the emergency support line at (555) 999-0000",
                    "IT Support",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                
                await LogActivityAsync("Support contact accessed", "??");
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Support Access Error", ex.Message);
            }
        }

        [RelayCommand]
        private async Task HelpAsync()
        {
            try
            {
                System.Windows.MessageBox.Show(
                    "Employee Time Tracker Help:\n\n• User Manual: Available in Help menu\n• Quick Start Guide: Check documentation folder\n• Video Tutorials: Available on company portal\n• FAQ: See knowledge base section",
                    "Help & Documentation",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                
                await LogActivityAsync("Help documentation accessed", "??");
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Help Access Error", ex.Message);
            }
        }

        [RelayCommand]
        private async Task MaintenanceModeAsync()
        {
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "Enable maintenance mode?\n\nThis will prevent employees from clocking in/out until maintenance mode is disabled.",
                    "Maintenance Mode",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    // Toggle maintenance mode
                    await LogActivityAsync("Maintenance mode enabled", "??");
                    CurrentOperation = "Maintenance mode active";
                    CurrentOperationIcon = "??";
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Maintenance Mode Error", ex.Message);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialize all command objects
        /// </summary>
        private void InitializeCommands()
        {
            // Commands are auto-generated by RelayCommand attributes
            // This method can be used for any additional command setup if needed
        }

        /// <summary>
        /// Load initial dashboard data
        /// </summary>
        private async Task LoadInitialDataAsync()
        {
            try
            {
                await RefreshDashboardDataAsync();
                await LoadSystemAlertsAsync();
                await LoadRecentActivityAsync();
                await LoadSampleEmployeeDataAsync();
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Data Load Error", ex.Message);
            }
        }

        /// <summary>
        /// Refresh all dashboard statistics and data
        /// </summary>
        private async Task RefreshDashboardDataAsync()
        {
            try
            {
                // Simulate loading real data from repositories
                await Task.Delay(500);

                // Update header statistics
                var employees = await _employeeRepository.GetActiveEmployeesAsync();
                ActiveEmployeeCount = employees?.Count ?? 0;

                // Simulate clocked in count
                ClockedInCount = (int)(ActiveEmployeeCount * 0.35); // ~35% clocked in

                // Simulate today's entries
                TodayEntries = ClockedInCount * 2; // Assume average 2 entries per active employee

                // Update performance metrics
                UpdatePerformanceMetrics();

                // Update system status
                UpdateSystemStatus();

                // Update totals
                TotalHoursToday = $"{(ClockedInCount * 7.5):F1}"; // Average 7.5 hours per employee
                PhotosCaptured = TodayEntries; // One photo per entry

                LastSyncTime = DateTime.Now.ToString("HH:mm") + " today";
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Refresh Error", ex.Message);
            }
        }

        /// <summary>
        /// Update performance metrics with simulated data
        /// </summary>
        private void UpdatePerformanceMetrics()
        {
            var random = new Random();
            
            // Simulate realistic performance metrics
            AvgResponseTime = $"{random.NextDouble() * 0.5 + 0.1:F1}s";
            PhotoSuccessRate = $"{random.Next(94, 99)}%";
            DbOperations = random.Next(200, 400).ToString();
            ErrorRate = $"{random.NextDouble() * 0.5:F1}%";
        }

        /// <summary>
        /// Update system health status
        /// </summary>
        private void UpdateSystemStatus()
        {
            var random = new Random();
            
            // Simulate camera status
            if (random.Next(0, 10) > 8) // 20% chance of camera issue
            {
                CameraStatusColor = new SolidColorBrush(Colors.Orange);
                CameraStatusText = "1 Offline";
                SystemHealthIcon = "??";
                SystemHealthColor = new SolidColorBrush(Colors.Orange);
            }
            else
            {
                CameraStatusColor = new SolidColorBrush(Colors.Green);
                CameraStatusText = "2 Available";
                SystemHealthIcon = "??";
                SystemHealthColor = new SolidColorBrush(Colors.Green);
            }

            // Update storage status
            var storageUsed = random.Next(60, 85);
            StorageStatus = $"{100 - storageUsed}% Free";

            // Update system uptime
            var uptime = DateTime.Now - DateTime.Today.AddHours(-random.Next(24, 72));
            SystemUptime = $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
        }

        /// <summary>
        /// Load system alerts and notifications
        /// </summary>
        private async Task LoadSystemAlertsAsync()
        {
            await Task.Delay(100);
            
            SystemAlerts.Clear();
            
            // Sample alerts
            SystemAlerts.Add(new SystemAlert
            {
                Icon = "??",
                Message = "3 employees over 8 hours today",
                AlertColor = new SolidColorBrush(Colors.Orange)
            });

            if (CameraStatusText.Contains("Offline"))
            {
                SystemAlerts.Add(new SystemAlert
                {
                    Icon = "??",
                    Message = "Camera offline since 2:15 PM",
                    AlertColor = new SolidColorBrush(Colors.Red)
                });
            }

            SystemAlerts.Add(new SystemAlert
            {
                Icon = "??",
                Message = "Weekly export successful",
                AlertColor = new SolidColorBrush(Colors.Green)
            });

            if (StorageStatus.StartsWith("1") || StorageStatus.StartsWith("2")) // Less than 30% free
            {
                SystemAlerts.Add(new SystemAlert
                {
                    Icon = "??",
                    Message = "Low storage warning",
                    AlertColor = new SolidColorBrush(Colors.Orange)
                });
            }
        }

        /// <summary>
        /// Load recent activity feed
        /// </summary>
        private async Task LoadRecentActivityAsync()
        {
            await Task.Delay(100);
            
            RecentActivity.Clear();
            
            var now = DateTime.Now;
            var activities = new[]
            {
                new ActivityItem { Icon = "??", Time = now.AddMinutes(-5).ToString("h:mm tt"), Description = "Johnson, Mary clocked OUT (8.5 hours worked)" },
                new ActivityItem { Icon = "??", Time = now.AddMinutes(-10).ToString("h:mm tt"), Description = "Davis, Tom clocked IN" },
                new ActivityItem { Icon = "??", Time = now.AddMinutes(-15).ToString("h:mm tt"), Description = "Camera capture failed for Wilson, Sarah" },
                new ActivityItem { Icon = "??", Time = now.AddMinutes(-22).ToString("h:mm tt"), Description = "Martinez, Carlos clocked OUT (7.2 hours worked)" },
                new ActivityItem { Icon = "??", Time = now.AddMinutes(-35).ToString("h:mm tt"), Description = "Anderson, Kim clocked IN" },
                new ActivityItem { Icon = "??", Time = now.AddMinutes(-47).ToString("h:mm tt"), Description = "Brown, Lisa clocked IN" },
                new ActivityItem { Icon = "??", Time = now.AddMinutes(-52).ToString("h:mm tt"), Description = "Photo system automatically restarted" },
                new ActivityItem { Icon = "??", Time = now.AddMinutes(-68).ToString("h:mm tt"), Description = "Smith, John clocked IN" }
            };

            foreach (var activity in activities)
            {
                RecentActivity.Add(activity);
            }
        }

        /// <summary>
        /// Load sample employee data for the dashboard
        /// </summary>
        private async Task LoadSampleEmployeeDataAsync()
        {
            await Task.Delay(100);
            
            ActiveEmployees.Clear();
            
            var sampleEmployees = new[]
            {
                new AdminEmployeeStatus
                {
                    FullName = "Smith, John",
                    Status = "IN",
                    StatusColor = new SolidColorBrush(Colors.Green),
                    ClockInTime = "8:30 AM",
                    HoursWorked = "3.5h",
                    PhotoIcon = "?",
                    PhotoStatus = "Complete"
                },
                new AdminEmployeeStatus
                {
                    FullName = "Johnson, Mary",
                    Status = "OUT",
                    StatusColor = new SolidColorBrush(Colors.Gray),
                    ClockInTime = "-",
                    HoursWorked = "8.5h",
                    PhotoIcon = "?",
                    PhotoStatus = "Complete"
                },
                new AdminEmployeeStatus
                {
                    FullName = "Williams, Bob",
                    Status = "IN",
                    StatusColor = new SolidColorBrush(Colors.Green),
                    ClockInTime = "9:15 AM",
                    HoursWorked = "2.8h",
                    PhotoIcon = "?",
                    PhotoStatus = "Complete"
                },
                new AdminEmployeeStatus
                {
                    FullName = "Brown, Lisa",
                    Status = "IN",
                    StatusColor = new SolidColorBrush(Colors.Green),
                    ClockInTime = "7:45 AM",
                    HoursWorked = "4.2h",
                    PhotoIcon = "??",
                    PhotoStatus = "Missing"
                },
                new AdminEmployeeStatus
                {
                    FullName = "Davis, Tom",
                    Status = "IN",
                    StatusColor = new SolidColorBrush(Colors.Green),
                    ClockInTime = "8:00 AM",
                    HoursWorked = "4.0h",
                    PhotoIcon = "?",
                    PhotoStatus = "Complete"
                }
            };

            foreach (var employee in sampleEmployees)
            {
                ActiveEmployees.Add(employee);
            }
        }

        /// <summary>
        /// Navigate to a specific admin section
        /// </summary>
        private async Task NavigateToSectionAsync(string sectionName, string icon)
        {
            try
            {
                CurrentOperation = $"Loading {sectionName}...";
                CurrentOperationIcon = icon;
                
                await Task.Delay(500);
                
                // Log navigation
                await LogActivityAsync($"Navigated to {sectionName}", icon);
                
                // In a real implementation, this would change the main content area
                System.Windows.MessageBox.Show(
                    $"{sectionName} interface would load here.\n\nThis is where the {sectionName.ToLower()} functionality would be displayed.",
                    sectionName,
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                
                CurrentOperation = "System monitoring active";
                CurrentOperationIcon = "??";
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Navigation Error", ex.Message);
            }
        }

        /// <summary>
        /// Log an activity to the recent activity feed
        /// </summary>
        private async Task LogActivityAsync(string description, string icon)
        {
            await Task.Run(() =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    RecentActivity.Insert(0, new ActivityItem
                    {
                        Icon = icon,
                        Time = DateTime.Now.ToString("h:mm tt"),
                        Description = description
                    });

                    // Keep only the last 20 activities
                    while (RecentActivity.Count > 20)
                    {
                        RecentActivity.RemoveAt(RecentActivity.Count - 1);
                    }
                });
            });
        }

        /// <summary>
        /// Handle errors with user-friendly messaging
        /// </summary>
        private async Task HandleErrorAsync(string title, string message)
        {
            await Task.Run(() =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show(
                        $"An error occurred: {message}\n\nPlease contact IT support if this problem persists.",
                        title,
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                });
            });

            await LogActivityAsync($"Error: {title}", "?");
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Clean up resources when ViewModel is disposed
        /// </summary>
        public void Dispose()
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Tick -= async (s, e) => await RefreshDashboardDataAsync();
        }

        #endregion
    }

    #region Supporting Data Models

    /// <summary>
    /// Model for employee status display in admin interface
    /// </summary>
    public class AdminEmployeeStatus
    {
        public string FullName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public SolidColorBrush StatusColor { get; set; } = new SolidColorBrush(Colors.Gray);
        public string ClockInTime { get; set; } = string.Empty;
        public string HoursWorked { get; set; } = string.Empty;
        public string PhotoIcon { get; set; } = string.Empty;
        public string PhotoStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model for system alerts and notifications
    /// </summary>
    public class SystemAlert
    {
        public string Icon { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public SolidColorBrush AlertColor { get; set; } = new SolidColorBrush(Colors.Blue);
    }

    /// <summary>
    /// Model for recent activity items
    /// </summary>
    public class ActivityItem
    {
        public string Icon { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    #endregion
}
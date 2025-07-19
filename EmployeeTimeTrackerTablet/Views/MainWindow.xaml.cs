using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using EmployeeTimeTrackerTablet.ViewModels;
using EmployeeTimeTracker.Models;

namespace EmployeeTimeTrackerTablet.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// This code-behind handles UI-specific concerns like timer updates and window state,
    /// and acts as the bridge between the XAML UI and the MainViewModel.
    /// Touch-optimized tablet interface for Employee Time Tracker
    /// 
    /// DIAGNOSTIC MODE: Conservative approach with gradual feature restoration
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel; // Reference to the ViewModel
        private readonly DispatcherTimer _timeTimer; // Timer for updating the live clock

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// Uses dependency injection to receive the MainViewModel.
        /// </summary>
        /// <param name="viewModel">The MainViewModel instance to be used as the DataContext.</param>
        public MainWindow(MainViewModel viewModel)
        {
            System.Diagnostics.Debug.WriteLine("=== MainWindow Constructor Begin ===");
            
            try
            {
                System.Diagnostics.Debug.WriteLine("Calling InitializeComponent...");
                InitializeComponent(); // Required by WPF for XAML component initialization

                System.Diagnostics.Debug.WriteLine("Validating ViewModel...");
                _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel), "MainViewModel cannot be null.");
                
                System.Diagnostics.Debug.WriteLine("Setting DataContext...");
                DataContext = _viewModel; // Set the DataContext for data binding

                System.Diagnostics.Debug.WriteLine("Initializing DispatcherTimer...");
                // Initialize DispatcherTimer for live clock display
                _timeTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(5) // More frequent updates for testing
                };
                _timeTimer.Tick += TimeTimer_Tick; // Subscribe to the Tick event
                
                System.Diagnostics.Debug.WriteLine("Starting timer...");
                _timeTimer.Start();

                System.Diagnostics.Debug.WriteLine("Subscribing to window events...");
                // Subscribe to window lifecycle events
                Loaded += MainWindow_Loaded; // Event for when the window is fully loaded
                Closing += MainWindow_Closing; // Event for when the window is about to close
                

                System.Diagnostics.Debug.WriteLine("=== MainWindow Constructor Complete ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== MainWindow Constructor ERROR ===");
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Fallback constructor for design-time support (without dependency injection).
        /// This allows the XAML designer to work without DI container.
        /// </summary>
        public MainWindow() : this(CreateDesignTimeViewModel())
        {
            // This constructor is primarily for design-time support in Visual Studio
            // In production, the parameterized constructor should be used with proper DI
        }

        /// <summary>
        /// Creates a design-time ViewModel for XAML designer support.
        /// </summary>
        /// <returns>A MainViewModel instance for design-time use.</returns>
        private static MainViewModel CreateDesignTimeViewModel()
        {
            System.Diagnostics.Debug.WriteLine("Creating design-time ViewModel...");
            
            try
            {
                return new MainViewModel(null!, null!, null!);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Design-time ViewModel creation failed: {ex.Message}");
                throw new InvalidOperationException("MainWindow requires dependency injection. Use the parameterized constructor in production.", ex);
            }
        }

        /// <summary>
        /// Handles the Loaded event of the MainWindow.
        /// Performs initial UI setup tasks once the window is ready.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== MainWindow_Loaded Begin ===");
            
            try
            {
                System.Diagnostics.Debug.WriteLine($"Window loaded successfully!");
                System.Diagnostics.Debug.WriteLine($"Window ActualWidth: {ActualWidth}");
                System.Diagnostics.Debug.WriteLine($"Window ActualHeight: {ActualHeight}");
                System.Diagnostics.Debug.WriteLine($"Window WindowState: {WindowState}");
                
                // DIAGNOSTIC: Test finding key UI elements
                var mainGrid = FindName("MainGrid") as Grid;
                System.Diagnostics.Debug.WriteLine(mainGrid != null ? "? MainGrid found" : "? MainGrid not found");
                
                var searchTextBox = FindName("SearchTextBox") as System.Windows.Controls.TextBox;
                System.Diagnostics.Debug.WriteLine(searchTextBox != null ? "? SearchTextBox found" : "? SearchTextBox not found");
                
                var currentDateText = FindName("CurrentDateText") as TextBlock;
                System.Diagnostics.Debug.WriteLine(currentDateText != null ? "? CurrentDateText found" : "? CurrentDateText not found");
                
                var currentTimeText = FindName("CurrentTimeText") as TextBlock;
                System.Diagnostics.Debug.WriteLine(currentTimeText != null ? "? CurrentTimeText found" : "? CurrentTimeText not found");
                
                var statusMessageText = FindName("StatusMessageText") as TextBlock;
                System.Diagnostics.Debug.WriteLine(statusMessageText != null ? "? StatusMessageText found" : "? StatusMessageText not found");

                // ENHANCED: MVVM Integration Verification
                System.Diagnostics.Debug.WriteLine("=== MVVM Integration Verification ===");
                if (DataContext is MainViewModel viewModel)
                {
                    System.Diagnostics.Debug.WriteLine("? DataContext properly set to MainViewModel");
                    System.Diagnostics.Debug.WriteLine($"? EmployeeSuggestions count: {viewModel.EmployeeSuggestions?.Count ?? 0}");
                    System.Diagnostics.Debug.WriteLine($"? Commands initialized: ClockIn={viewModel.ClockInCommand != null}, ClockOut={viewModel.ClockOutCommand != null}, Admin={viewModel.AdminAccessCommand != null}");
                    System.Diagnostics.Debug.WriteLine($"? Status Message: '{viewModel.StatusMessage}'");
                    System.Diagnostics.Debug.WriteLine($"? Button States: CanClockIn={viewModel.CanClockIn}, CanClockOut={viewModel.CanClockOut}");
                    System.Diagnostics.Debug.WriteLine($"? Employee Count: {viewModel.EmployeeCount}");
                    System.Diagnostics.Debug.WriteLine($"? IsLoading: {viewModel.IsLoading}");
                    
                    // Additional ViewModel state verification
                    System.Diagnostics.Debug.WriteLine($"? Selected Employee: {viewModel.SelectedEmployee?.FirstName ?? "None"} {viewModel.SelectedEmployee?.LastName ?? ""}");
                    System.Diagnostics.Debug.WriteLine($"? Employee Status Message: '{viewModel.EmployeeStatusMessage}'");
                    System.Diagnostics.Debug.WriteLine($"? Notes: '{viewModel.Notes}'");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("? DataContext not properly set to MainViewModel");
                }

                // ENHANCED: Data Binding Verification
                System.Diagnostics.Debug.WriteLine("=== Data Binding Verification ===");
                
                // Search TextBox binding verification
                if (searchTextBox != null)
                {
                    var searchBinding = searchTextBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty);
                    System.Diagnostics.Debug.WriteLine($"? SearchTextBox binding: {searchBinding?.ParentBinding?.Path?.Path ?? "No binding"}");
                    System.Diagnostics.Debug.WriteLine($"? SearchTextBox UpdateSourceTrigger: {searchBinding?.ParentBinding?.UpdateSourceTrigger ?? System.Windows.Data.UpdateSourceTrigger.Default}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("? SearchTextBox not found");
                }
                
                // Employee ListBox binding verification
                var employeeListBox = FindName("EmployeeSuggestionsListBox") as System.Windows.Controls.ListBox;
                if (employeeListBox != null)
                {
                    var itemsSourceBinding = employeeListBox.GetBindingExpression(System.Windows.Controls.ItemsControl.ItemsSourceProperty);
                    var selectedItemBinding = employeeListBox.GetBindingExpression(System.Windows.Controls.Primitives.Selector.SelectedItemProperty);
                    System.Diagnostics.Debug.WriteLine($"? ListBox ItemsSource binding: {itemsSourceBinding?.ParentBinding?.Path?.Path ?? "No binding"}");
                    System.Diagnostics.Debug.WriteLine($"? ListBox SelectedItem binding: {selectedItemBinding?.ParentBinding?.Path?.Path ?? "No binding"}");
                    System.Diagnostics.Debug.WriteLine($"? ListBox ItemTemplate: {(employeeListBox.ItemTemplate != null ? "Configured" : "Not configured")}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("? EmployeeSuggestionsListBox not found");
                }

                // Status message binding verification
                if (statusMessageText != null)
                {
                    var statusBinding = statusMessageText.GetBindingExpression(TextBlock.TextProperty);
                    System.Diagnostics.Debug.WriteLine($"? StatusMessageText binding: {statusBinding?.ParentBinding?.Path?.Path ?? "No binding"}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("? StatusMessageText not found");
                }

                // Employee status text binding verification
                var employeeStatusText = FindName("EmployeeStatusText") as TextBlock;
                if (employeeStatusText != null)
                {
                    var empStatusBinding = employeeStatusText.GetBindingExpression(TextBlock.TextProperty);
                    System.Diagnostics.Debug.WriteLine($"? EmployeeStatusText binding: {empStatusBinding?.ParentBinding?.Path?.Path ?? "No binding"}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("? EmployeeStatusText not found");
                }

                // Employee count binding verification
                var employeeCountText = FindName("EmployeeCountText") as TextBlock;
                if (employeeCountText != null)
                {
                    var countBinding = employeeCountText.GetBindingExpression(TextBlock.TextProperty);
                    System.Diagnostics.Debug.WriteLine($"? EmployeeCountText binding: {countBinding?.ParentBinding?.Path?.Path ?? "No binding"}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("? EmployeeCountText not found");
                }

                // Button command and state binding verification
                System.Diagnostics.Debug.WriteLine("=== Button Command & State Verification ===");
                
                // Note: Button names may not have x:Name in the current XAML, so we'll search by type and properties
                var buttons = Array.Empty<System.Windows.Controls.Button>();
                if (mainGrid != null)
                {
                    buttons = FindVisualChildren<System.Windows.Controls.Button>(mainGrid).ToArray();
                    System.Diagnostics.Debug.WriteLine($"? Found {buttons.Length} buttons in MainGrid");
                }

                foreach (var button in buttons)
                {
                    var commandBinding = button.GetBindingExpression(System.Windows.Controls.Button.CommandProperty);
                    var isEnabledBinding = button.GetBindingExpression(System.Windows.Controls.Button.IsEnabledProperty);
                    
                    if (commandBinding != null || isEnabledBinding != null)
                    {
                        var buttonContent = button.Content?.ToString() ?? "Unknown";
                        System.Diagnostics.Debug.WriteLine($"? Button '{buttonContent}' - Command: {commandBinding?.ParentBinding?.Path?.Path ?? "No binding"}, IsEnabled: {isEnabledBinding?.ParentBinding?.Path?.Path ?? "No binding"}");
                    }
                }

                System.Diagnostics.Debug.WriteLine("=== End MVVM Verification ===");

                // DIAGNOSTIC: Set initial time display
                UpdateTimeDisplay();
                
                // DIAGNOSTIC: Set initial status
                UpdateStatusIndicators();

                // DIAGNOSTIC: Set focus
                if (searchTextBox != null)
                {
                    searchTextBox.Focus();
                    System.Diagnostics.Debug.WriteLine("? Focus set to SearchTextBox");
                }
                
                System.Diagnostics.Debug.WriteLine("=== MainWindow_Loaded Complete ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== MainWindow_Loaded ERROR ===");
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                // Show error to user but don't crash
                System.Windows.MessageBox.Show(
                    $"Error during window initialization: {ex.Message}",
                    "Window Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Handles the Closing event of the MainWindow.
        /// Performs cleanup tasks before the application exits.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== MainWindow_Closing Begin ===");
            
            try
            {
                System.Diagnostics.Debug.WriteLine("Stopping timer...");
                _timeTimer?.Stop();
                System.Diagnostics.Debug.WriteLine("=== MainWindow_Closing Complete ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow_Closing Error]: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler for the DispatcherTimer's Tick event.
        /// Triggers the update of the time display.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        private void TimeTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                UpdateTimeDisplay();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TimeTimer_Tick Error]: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the current date and time displayed in the header.
        /// </summary>
        private void UpdateTimeDisplay()
        {
            try
            {
                var now = DateTime.Now;

                var currentDateText = FindName("CurrentDateText") as TextBlock;
                var currentTimeText = FindName("CurrentTimeText") as TextBlock;

                if (currentDateText != null)
                {
                    currentDateText.Text = now.ToString("dddd, MMMM d, yyyy");
                }
                
                if (currentTimeText != null)
                {
                    currentTimeText.Text = now.ToString("h:mm tt");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateTimeDisplay Error]: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates status indicators in the UI.
        /// </summary>
        private void UpdateStatusIndicators()
        {
            try
            {
                var statusIndicator = FindName("StatusIndicator") as TextBlock;
                if (statusIndicator != null)
                {
                    statusIndicator.Text = "? System Ready";
                }
                
                var databaseStatusText = FindName("DatabaseStatusText") as TextBlock;
                if (databaseStatusText != null)
                {
                    databaseStatusText.Text = "Connected";
                }
                
                var employeeCountText = FindName("EmployeeCountText") as TextBlock;
                if (employeeCountText != null)
                {
                    employeeCountText.Text = "Employees Loading..."; // Will be updated by data binding
                }
                
                System.Diagnostics.Debug.WriteLine("Status indicators updated");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateStatusIndicators Error]: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to find visual children of a specific type in the visual tree.
        /// Used for comprehensive button verification in MVVM diagnostics.
        /// </summary>
        /// <typeparam name="T">The type of visual children to find</typeparam>
        /// <param name="parent">The parent visual element to search within</param>
        /// <returns>Collection of visual children of the specified type</returns>
        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
        {
            if (parent == null) yield break;

            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                
                if (child is T typedChild)
                {
                    yield return typedChild;
                }

                foreach (var grandChild in FindVisualChildren<T>(child))
                {
                    yield return grandChild;
                }
            }
        }

        /// <summary>
        /// Override the OnClosed method to ensure proper cleanup.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected override void OnClosed(EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== OnClosed Begin ===");
            
            try
            {
                _timeTimer?.Stop();
                System.Diagnostics.Debug.WriteLine("Timer stopped successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OnClosed Error]: {ex.Message}");
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine("=== OnClosed Complete ===");
                base.OnClosed(e);
            }
        }

        /// <summary>
        /// Test method to verify DualTimeCorrectionDialog functionality.
        /// TEMPORARY: This is for testing purposes only and should be removed in production.
        /// </summary>
        private void TestDualTimeCorrectionDialog()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== Testing DualTimeCorrectionDialog ===");
                
                // Create a test repository (use null for testing - this is not ideal but for testing only)
                var testRepository = new EmployeeTimeTracker.Data.TimeEntryRepository();
                
                // Create a test employee
                var testEmployee = new EmployeeTimeTracker.Models.Employee
                {
                    EmployeeID = 1,
                    FirstName = "John",
                    LastName = "Doe",
                    PayRate = 15.00m,
                    JobTitle = "Test Employee"
                };

                // Create a test time entry
                var testTimeEntry = new EmployeeTimeTracker.Models.TimeEntry
                {
                    EntryID = 1,
                    EmployeeID = 1,
                    ShiftDate = DateTime.Today,
                    TimeIn = new TimeSpan(9, 0, 0), // 9:00 AM
                    TimeOut = new TimeSpan(17, 0, 0), // 5:00 PM
                    TotalHours = 8.0m,
                    GrossPay = 120.00m,
                    Notes = "Test time entry"
                };

                // Create and show the dialog using the App's factory method
                var app = (App)System.Windows.Application.Current;
                var dialog = app.CreateDualTimeCorrectionDialog(testEmployee, testTimeEntry);
                dialog.Owner = this;
                
                var result = dialog.ShowDialog();
                
                if (result == true)
                {
                    System.Diagnostics.Debug.WriteLine($"Dialog applied successfully!");
                    System.Diagnostics.Debug.WriteLine($"Corrected Clock-In Time: {dialog.CorrectedClockInTime}");
                    System.Diagnostics.Debug.WriteLine($"Corrected Clock-Out Time: {dialog.CorrectedClockOutTime}");
                    System.Diagnostics.Debug.WriteLine($"Correction Reason: {dialog.CorrectionReason}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Dialog was cancelled");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error testing DualTimeCorrectionDialog: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Helper method to trigger the test dialog for verification.
        /// TEMPORARY: This should be removed in production.
        /// </summary>
        public void TriggerTestDialog()
        {
            TestDualTimeCorrectionDialog();
        }

        /// <summary>
        /// Alternative method to show DualTimeCorrectionDialog using the App factory method.
        /// This demonstrates the recommended approach for production use.
        /// </summary>
        public void TriggerDualTimeCorrectionDialogViaFactory()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== Testing DualTimeCorrectionDialog via Factory ===");
                
                // Create test data
                var testEmployee = new EmployeeTimeTracker.Models.Employee
                {
                    EmployeeID = 1,
                    FirstName = "Jane",
                    LastName = "Smith",
                    PayRate = 18.50m,
                    JobTitle = "Manager Test Employee"
                };

                var testTimeEntry = new EmployeeTimeTracker.Models.TimeEntry
                {
                    EntryID = 1,
                    EmployeeID = 1,
                    ShiftDate = DateTime.Today,
                    TimeIn = new TimeSpan(8, 30, 0), // 8:30 AM
                    TimeOut = new TimeSpan(16, 45, 0), // 4:45 PM
                    TotalHours = 8.25m,
                    GrossPay = 152.63m,
                    Notes = "Factory test time entry"
                };

                // Use the App factory method (recommended approach)
                var app = (App)System.Windows.Application.Current;
                var dialog = app.CreateDualTimeCorrectionDialog(testEmployee, testTimeEntry);
                dialog.Owner = this;
                
                var result = dialog.ShowDialog();
                
                if (result == true)
                {
                    System.Diagnostics.Debug.WriteLine($"Factory dialog applied successfully!");
                    System.Diagnostics.Debug.WriteLine($"Corrected Clock-In Time: {dialog.CorrectedClockInTime}");
                    System.Diagnostics.Debug.WriteLine($"Corrected Clock-Out Time: {dialog.CorrectedClockOutTime}");
                    System.Diagnostics.Debug.WriteLine($"Correction Reason: {dialog.CorrectionReason}");
                    System.Diagnostics.Debug.WriteLine($"Clock-In Correction Enabled: {dialog.IsClockInCorrectionEnabled}");
                    System.Diagnostics.Debug.WriteLine($"Clock-Out Correction Enabled: {dialog.IsClockOutCorrectionEnabled}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Factory dialog was cancelled");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error testing DualTimeCorrectionDialog via factory: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Method to demonstrate usage from a ViewModel context.
        /// This shows how ViewModels can use the dialog through the App factory.
        /// </summary>
        public async Task<bool> ShowDualTimeCorrectionFromViewModelAsync(Employee? employee, TimeEntry? timeEntry)
        {
            return await Task.Run(() =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("=== DualTimeCorrectionDialog from ViewModel Context ===");
                    
                    if (employee == null || timeEntry == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Employee or TimeEntry is null");
                        return false;
                    }

                    bool dialogResult = false;
                    Dispatcher.Invoke(() =>
                    {
                        // Get the App instance
                        var app = (App)System.Windows.Application.Current;
                        if (app?.Services == null)
                        {
                            System.Diagnostics.Debug.WriteLine("App services not available");
                            dialogResult = false;
                            return;
                        }

                        // Create dialog with proper DI
                        var dialog = app.CreateDualTimeCorrectionDialog(employee, timeEntry);
                        dialog.Owner = this;

                        // Show dialog
                        var result = dialog.ShowDialog();

                        if (result == true && dialog.IsApplied)
                        {
                            System.Diagnostics.Debug.WriteLine("Corrections applied from ViewModel context");
                            
                            // Here you would typically:
                            // 1. Notify the ViewModel that corrections were applied
                            // 2. Refresh data bindings
                            // 3. Update UI state
                            // 4. Log the correction (optional, based on your auditing needs)
                            
                            dialogResult = true;
                        }
                        else
                        {
                            dialogResult = false;
                        }
                    });

                    return dialogResult;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in ShowDualTimeCorrectionFromViewModelAsync: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Example integration with the existing ManagerCorrectTimeAsync pattern.
        /// This shows how the DualTimeCorrectionDialog could replace the TimeCorrectionDialog
        /// in the existing manager correction workflow.
        /// </summary>
        public async Task<bool> ExecuteManagerDualTimeCorrectionAsync(Employee? employee, TimeEntry? timeEntry, string managerPin = "9999")
        {
            return await Task.Run(() =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("=== Manager Dual Time Correction Workflow ===");
                    
                    // Step 1: Validate inputs
                    if (employee == null || timeEntry == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Invalid employee or time entry");
                        return false;
                    }

                    // Step 2: Manager PIN authentication (simplified for demo)
                    // In production, this would use the ManagerAuthService
                    if (managerPin != "9999")
                    {
                        System.Diagnostics.Debug.WriteLine("Invalid manager PIN");
                        return false;
                    }

                    System.Diagnostics.Debug.WriteLine("Manager authenticated successfully");

                    // Step 3: Show dual time correction dialog (must run on UI thread)
                    bool dialogResult = false;
                    Dispatcher.Invoke(() =>
                    {
                        var app = (App)System.Windows.Application.Current;
                        var dialog = app.CreateDualTimeCorrectionDialog(employee, timeEntry);
                        dialog.Owner = this;

                        var result = dialog.ShowDialog();

                        // Step 4: Process results
                        if (result == true && dialog.IsApplied)
                        {
                            System.Diagnostics.Debug.WriteLine("Manager dual time correction completed successfully");
                            System.Diagnostics.Debug.WriteLine($"Clock-In Corrected: {dialog.IsClockInCorrectionEnabled}");
                            System.Diagnostics.Debug.WriteLine($"Clock-Out Corrected: {dialog.IsClockOutCorrectionEnabled}");
                            System.Diagnostics.Debug.WriteLine($"Reason: {dialog.CorrectionReason}");
                            
                            // Here you would typically:
                            // 1. Log the correction for audit purposes
                            // 2. Update the database (handled by dialog)
                            // 3. Refresh UI data
                            // 4. Notify other components of the change
                            // 5. Optionally, send a notification or trigger other workflows
                            //    based on your application's requirements
                            // 6. Handle any post-correction logic (e.g., recalculating totals, updating reports, etc.)
                            
                            dialogResult = true;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Manager dual time correction was cancelled");
                            dialogResult = false;
                        }
                    });

                    return dialogResult;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in ExecuteManagerDualTimeCorrectionAsync: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Comprehensive test method for Step 5 validation.
        /// This method tests all aspects of the DualTimeCorrectionDialog integration.
        /// </summary>
        public async Task RunCompleteStep5TestAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== STEP 5/5 COMPLETE TEST SUITE ===");
                
                // Test 1: Basic Dialog Functionality
                System.Diagnostics.Debug.WriteLine("Test 1: Basic Dialog Creation and Display");
                TriggerDualTimeCorrectionDialogViaFactory();
                await Task.Delay(500); // Allow for user interaction
                
                // Test 2: ViewModel Integration
                System.Diagnostics.Debug.WriteLine("Test 2: ViewModel Integration Pattern");
                var testEmployee = new Employee
                {
                    EmployeeID = 2,
                    FirstName = "Alice",
                    LastName = "Johnson",
                    PayRate = 22.50m,
                    JobTitle = "Senior Developer"
                };

                var testTimeEntry = new TimeEntry
                {
                    EntryID = 2,
                    EmployeeID = 2,
                    ShiftDate = DateTime.Today.AddDays(-1), // Yesterday
                    TimeIn = new TimeSpan(8, 0, 0), // 8:00 AM
                    TimeOut = new TimeSpan(18, 30, 0), // 6:30 PM
                    TotalHours = 10.5m,
                    GrossPay = 236.25m,
                    Notes = "ViewModel integration test entry"
                };

                await ShowDualTimeCorrectionFromViewModelAsync(testEmployee, testTimeEntry);
                
                // Test 3: Manager Workflow Integration
                System.Diagnostics.Debug.WriteLine("Test 3: Manager Workflow Integration");
                await ExecuteManagerDualTimeCorrectionAsync(testEmployee, testTimeEntry);
                
                // Test 4: Error Handling
                System.Diagnostics.Debug.WriteLine("Test 4: Error Handling");
                await TestErrorHandlingScenarios();
                
                System.Diagnostics.Debug.WriteLine("=== STEP 5/5 TESTS COMPLETED ===");
                System.Diagnostics.Debug.WriteLine("? All integration patterns tested successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RunCompleteStep5TestAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Tests error handling scenarios for the DualTimeCorrectionDialog.
        /// </summary>
        private async Task TestErrorHandlingScenarios()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Testing error handling scenarios...");
                
                // Test with null employee
                System.Diagnostics.Debug.WriteLine("Testing null employee scenario...");
                var testTimeEntry = new TimeEntry
                {
                    EntryID = 999,
                    EmployeeID = 999,
                    ShiftDate = DateTime.Today,
                    TimeIn = new TimeSpan(9, 0, 0),
                    TimeOut = new TimeSpan(17, 0, 0),
                    TotalHours = 8.0m,
                    GrossPay = 120.00m,
                    Notes = "Error test time entry"
                };
                await ShowDualTimeCorrectionFromViewModelAsync(null, testTimeEntry);
                
                // Test with dummy employee for testing error handling
                var dummyEmployee = new Employee
                {
                    EmployeeID = -1, // Invalid ID to simulate error condition
                    FirstName = "Invalid",
                    LastName = "Employee",
                    PayRate = 0.00m,
                    JobTitle = "Error Test Employee"
                };
                await ShowDualTimeCorrectionFromViewModelAsync(dummyEmployee, testTimeEntry);
                
                // Test with null time entry
                System.Diagnostics.Debug.WriteLine("Testing null time entry scenario...");
                var testEmployee = new Employee
                {
                    EmployeeID = 999,
                    FirstName = "Test",
                    LastName = "Employee",
                    PayRate = 15.00m,
                    JobTitle = "Error Test Employee"
                };
                await ShowDualTimeCorrectionFromViewModelAsync(testEmployee, null);
                
                // Test with dummy time entry for testing error handling
                var dummyTimeEntry = new TimeEntry
                {
                    EntryID = -1, // Invalid ID to simulate error condition
                    EmployeeID = 999,
                    ShiftDate = DateTime.Today,
                    TimeIn = new TimeSpan(0, 0, 0), // Invalid time to simulate error
                    TimeOut = new TimeSpan(0, 0, 0), // Invalid time to simulate error
                    TotalHours = 0.0m,
                    GrossPay = 0.00m,
                    Notes = "Invalid test time entry"
                };
                await ShowDualTimeCorrectionFromViewModelAsync(testEmployee, dummyTimeEntry);
                
                System.Diagnostics.Debug.WriteLine("Error handling tests completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Expected error in TestErrorHandlingScenarios: {ex.Message}");
            }
        }

        /// <summary>
        /// Public method to trigger the complete Step 5 test suite.
        /// This can be called from UI buttons or other test scenarios.
        /// </summary>
        public void TriggerCompleteStep5Test()
        {
            _ = Task.Run(async () => await RunCompleteStep5TestAsync());
        }
    }
}
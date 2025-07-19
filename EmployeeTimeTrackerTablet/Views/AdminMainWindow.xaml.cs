using System.Windows;
using System;
using System.Threading.Tasks;
using EmployeeTimeTrackerTablet.ViewModels;
using EmployeeTimeTracker.Models;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeTimeTrackerTablet.Views
{
    /// <summary>
    /// Interaction logic for AdminMainWindow.xaml
    /// CRITICAL FIX: Constructor updated to support dependency injection for AdminMainViewModel.
    /// ENHANCEMENT: Added DualTimeCorrectionDialog integration for advanced time correction.
    /// </summary>
    public partial class AdminMainWindow : Window
    {
        private readonly AdminMainViewModel? _viewModel;

        /// <summary>
        /// Initializes a new instance of the AdminMainWindow class with dependency injection support.
        /// </summary>
        /// <param name="viewModel">The AdminMainViewModel instance to be used as the DataContext.</param>
        public AdminMainWindow(AdminMainViewModel viewModel)
        {
            InitializeComponent();
            
            // Set the DataContext to the injected ViewModel
            _viewModel = viewModel ?? throw new System.ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;
        }

        /// <summary>
        /// Parameterless constructor for design-time support.
        /// This should not be used in production code.
        /// </summary>
        public AdminMainWindow()
        {
            InitializeComponent();
            
            // For design-time support only - this will be replaced by DI in production
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                // Design-time placeholder - actual ViewModel will be injected
                DataContext = null;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Opens the DualTimeCorrectionDialog for a selected employee and time entry.
        /// This method demonstrates proper integration with dependency injection.
        /// </summary>
        /// <param name="employee">The employee whose time is being corrected</param>
        /// <param name="timeEntry">The time entry to be corrected</param>
        /// <returns>True if corrections were applied, false otherwise</returns>
        public async Task<bool> OpenDualTimeCorrectionDialogAsync(Employee employee, TimeEntry timeEntry)
        {
            if (employee == null || timeEntry == null)
            {
                System.Windows.MessageBox.Show("Please select a valid employee and time entry for correction.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            try
            {
                // Access the application's service provider
                var app = (App)System.Windows.Application.Current;
                
                if (app?.Services == null)
                {
                    System.Windows.MessageBox.Show("Application services are not initialized.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Create the dialog using the factory method that handles dependency injection
                var dialog = app.CreateDualTimeCorrectionDialog(employee, timeEntry);
                dialog.Owner = this;

                // Show the dialog
                bool? dialogResult = dialog.ShowDialog();

                // If corrections were applied, handle the result
                if (dialogResult == true && dialog.IsApplied)
                {
                    System.Windows.MessageBox.Show("Time entry corrected successfully.", "Correction Applied", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Refresh the view model data to show updated information
                    await RefreshTimeEntriesAsync();
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening DualTimeCorrectionDialog: {ex.Message}");
                System.Windows.MessageBox.Show($"An error occurred while opening the correction dialog: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Example event handler for a "Correct Time Entry" button click.
        /// This demonstrates how to use the dialog with selected data from a DataGrid or similar control.
        /// </summary>
        private async void CorrectTimeEntryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get selected employee and time entry from your UI
                // This is a placeholder - replace with actual selection logic from your DataGrid or other controls
                var selectedEmployee = GetSelectedEmployee();
                var selectedTimeEntry = GetSelectedTimeEntry();

                if (selectedEmployee == null || selectedTimeEntry == null)
                {
                    System.Windows.MessageBox.Show("Please select an employee and time entry to correct.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Open the dual time correction dialog
                await OpenDualTimeCorrectionDialogAsync(selectedEmployee, selectedTimeEntry);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CorrectTimeEntryButton_Click: {ex.Message}");
                System.Windows.MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Gets the currently selected employee from the UI.
        /// Replace this with actual selection logic based on your UI implementation.
        /// </summary>
        /// <returns>The selected employee or null if none selected</returns>
        private Employee? GetSelectedEmployee()
        {
            // Example implementation - replace with your actual selection logic
            // If you have a DataGrid named TimeEntriesDataGrid:
            // var selectedItem = TimeEntriesDataGrid.SelectedItem as TimeEntryReportData;
            // return selectedItem?.Employee;
            
            // If you have a ComboBox for employee selection:
            // return EmployeeComboBox.SelectedItem as Employee;
            
            // For demonstration, return a test employee (remove in production)
            return CreateTestEmployee();
        }

        /// <summary>
        /// Gets the currently selected time entry from the UI.
        /// Replace this with actual selection logic based on your UI implementation.
        /// </summary>
        /// <returns>The selected time entry or null if none selected</returns>
        private TimeEntry? GetSelectedTimeEntry()
        {
            // Example implementation - replace with your actual selection logic
            // If you have a DataGrid named TimeEntriesDataGrid:
            // return TimeEntriesDataGrid.SelectedItem as TimeEntry;
            
            // If you get time entries from the view model:
            // return _viewModel?.SelectedTimeEntry;
            
            // For demonstration, return a test time entry (remove in production)
            return CreateTestTimeEntry();
        }

        /// <summary>
        /// Refreshes the time entries data after corrections are applied.
        /// Replace this with your actual data refresh logic.
        /// </summary>
        private async Task RefreshTimeEntriesAsync()
        {
            try
            {
                // Refresh data in your view model or reload from the database
                // Example: await _viewModel.LoadTimeEntriesAsync();
                // Example: await _viewModel.RefreshDataAsync();
                
                System.Diagnostics.Debug.WriteLine("Time entries data refreshed after correction");
                await Task.CompletedTask; // Placeholder for actual async refresh logic
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing time entries: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a test employee for demonstration purposes.
        /// REMOVE THIS METHOD IN PRODUCTION - Replace with actual employee selection.
        /// </summary>
        private Employee CreateTestEmployee()
        {
            return new Employee
            {
                EmployeeID = 1,
                FirstName = "John",
                LastName = "Doe",
                PayRate = 15.00m,
                JobTitle = "Test Employee",
                Active = true
            };
        }

        /// <summary>
        /// Creates a test time entry for demonstration purposes.
        /// REMOVE THIS METHOD IN PRODUCTION - Replace with actual time entry selection.
        /// </summary>
        private TimeEntry CreateTestTimeEntry()
        {
            return new TimeEntry
            {
                EntryID = 1,
                EmployeeID = 1,
                ShiftDate = DateTime.Today,
                TimeIn = new TimeSpan(9, 0, 0), // 9:00 AM
                TimeOut = new TimeSpan(17, 0, 0), // 5:00 PM
                TotalHours = 8.0m,
                GrossPay = 120.00m,
                Notes = "Test time entry for dual correction",
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };
        }

        /// <summary>
        /// Example method to demonstrate context menu integration.
        /// This shows how you might add a context menu to a DataGrid for time corrections.
        /// </summary>
        private void TimeEntryContextMenu_CorrectTime_Click(object sender, RoutedEventArgs e)
        {
            // This would typically be called from a context menu on a DataGrid row
            CorrectTimeEntryButton_Click(sender, e);
        }

        /// <summary>
        /// Example method for batch time corrections.
        /// This demonstrates how you might handle multiple time entry corrections.
        /// </summary>
        private async void BatchCorrectTimeEntriesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get multiple selected items from a DataGrid or list
                // var selectedEntries = GetSelectedTimeEntries();
                
                // Ensure this is truly async to resolve CS1998 warning
                await Task.Delay(1);
                
                System.Windows.MessageBox.Show("Batch time correction functionality can be implemented here.\nEach entry would be corrected individually using the DualTimeCorrectionDialog.", 
                               "Batch Correction", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Example of how to iterate through multiple corrections:
                /*
                foreach (var entry in selectedEntries)
                {
                    var employee = GetEmployeeForTimeEntry(entry);
                    var shouldContinue = await OpenDualTimeCorrectionDialogAsync(employee, entry);
                    if (!shouldContinue)
                        break; // User cancelled, stop batch operation
                }
                */
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in batch correction: {ex.Message}");
                System.Windows.MessageBox.Show($"Error during batch correction: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
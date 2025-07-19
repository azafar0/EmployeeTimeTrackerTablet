using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EmployeeTimeTrackerTablet.ViewModels;

namespace EmployeeTimeTrackerTablet.Views
{
    /// <summary>
    /// Interaction logic for AdminMainWindow.xaml
    /// This code-behind handles UI-specific concerns and diagnostic verification for the administrative interface.
    /// Acts as the bridge between the XAML UI and the AdminMainViewModel, following established MVVM patterns.
    /// Touch-optimized administrative interface for Employee Time Tracker system management.
    /// </summary>
    public partial class AdminMainWindow : Window
    {
        private readonly AdminMainViewModel _viewModel; // Reference to the ViewModel

        /// <summary>
        /// Initializes a new instance of the AdminMainWindow class.
        /// Uses dependency injection to receive the AdminMainViewModel.
        /// </summary>
        /// <param name="viewModel">The AdminMainViewModel instance to be used as the DataContext.</param>
        public AdminMainWindow(AdminMainViewModel viewModel)
        {
            Debug.WriteLine("=== AdminMainWindow Constructor Begin ===");
            
            try
            {
                Debug.WriteLine("AdminMainWindow: Calling InitializeComponent...");
                InitializeComponent(); // Required by WPF for XAML component initialization

                Debug.WriteLine("AdminMainWindow: Validating ViewModel...");
                _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel), "AdminMainViewModel cannot be null.");
                
                Debug.WriteLine("AdminMainWindow: Setting DataContext...");
                DataContext = _viewModel; // Set the DataContext for data binding

                Debug.WriteLine("AdminMainWindow: Subscribing to window events...");
                // Subscribe to window lifecycle events
                Loaded += AdminMainWindow_Loaded; // Event for when the window is fully loaded
                Closing += AdminMainWindow_Closing; // Event for when the window is about to close
                
                Debug.WriteLine("=== AdminMainWindow Constructor Complete ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"=== AdminMainWindow Constructor ERROR ===");
                Debug.WriteLine($"Exception: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Fallback constructor for design-time support (without dependency injection).
        /// This allows the XAML designer to work without DI container.
        /// </summary>
        public AdminMainWindow() : this(CreateDesignTimeViewModel())
        {
            // This constructor is primarily for design-time support in Visual Studio
            // In production, the parameterized constructor should be used with proper DI
        }

        /// <summary>
        /// Creates a design-time ViewModel for XAML designer support.
        /// </summary>
        /// <returns>An AdminMainViewModel instance for design-time use.</returns>
        private static AdminMainViewModel CreateDesignTimeViewModel()
        {
            Debug.WriteLine("AdminMainWindow: Creating design-time ViewModel...");
            
            try
            {
                // Create a design-time instance with null repositories for XAML designer
                return new AdminMainViewModel(null!, null!);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AdminMainWindow: Design-time ViewModel creation failed: {ex.Message}");
                throw new InvalidOperationException("AdminMainWindow requires dependency injection. Use the parameterized constructor in production.", ex);
            }
        }

        /// <summary>
        /// Handles the Loaded event of the AdminMainWindow.
        /// Performs initial UI setup tasks and comprehensive MVVM verification once the window is ready.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void AdminMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("=== AdminMainWindow_Loaded Begin ===");
            
            try
            {
                Debug.WriteLine($"AdminMainWindow: Window loaded successfully!");
                Debug.WriteLine($"AdminMainWindow: Window ActualWidth: {ActualWidth}");
                Debug.WriteLine($"AdminMainWindow: Window ActualHeight: {ActualHeight}");
                Debug.WriteLine($"AdminMainWindow: Window WindowState: {WindowState}");
                
                // DIAGNOSTIC: Test finding key UI elements
                VerifyUIElementsPresent();
                
                // ENHANCED: MVVM Integration Verification
                VerifyMVVMIntegration();
                
                // ENHANCED: Data Binding Verification
                VerifyDataBindings();
                
                // ENHANCED: Command Binding Verification
                VerifyCommandBindings();
                
                Debug.WriteLine("=== AdminMainWindow_Loaded Complete ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"=== AdminMainWindow_Loaded ERROR ===");
                Debug.WriteLine($"Exception: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                // Show error to user but don't crash
                MessageBox.Show(
                    $"Error during admin window initialization: {ex.Message}",
                    "Admin Window Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Handles the Closing event of the AdminMainWindow.
        /// Performs cleanup tasks before the window closes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void AdminMainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            Debug.WriteLine("=== AdminMainWindow_Closing Begin ===");
            
            try
            {
                Debug.WriteLine("AdminMainWindow: Performing cleanup...");
                
                // Dispose the ViewModel if it implements IDisposable
                if (_viewModel is IDisposable disposableViewModel)
                {
                    disposableViewModel.Dispose();
                    Debug.WriteLine("AdminMainWindow: ViewModel disposed successfully");
                }
                
                Debug.WriteLine("=== AdminMainWindow_Closing Complete ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminMainWindow_Closing Error]: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifies that key UI elements are present and accessible.
        /// </summary>
        private void VerifyUIElementsPresent()
        {
            Debug.WriteLine("=== UI Elements Verification ===");
            
            try
            {
                // Find the main grid
                var mainGrid = this.Content as Grid;
                Debug.WriteLine(mainGrid != null ? "? Main Grid found" : "? Main Grid not found");
                
                // Check for header elements
                var headerBorder = FindVisualChild<Border>(this);
                Debug.WriteLine(headerBorder != null ? "? Header Border found" : "? Header Border not found");
                
                // Count buttons in the window
                var buttons = FindVisualChildren<Button>(this).ToList();
                Debug.WriteLine($"? Found {buttons.Count} buttons in AdminMainWindow");
                
                // Check for specific admin UI elements
                var textBlocks = FindVisualChildren<TextBlock>(this).ToList();
                Debug.WriteLine($"? Found {textBlocks.Count} text blocks in AdminMainWindow");
                
                Debug.WriteLine("=== UI Elements Verification Complete ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VerifyUIElementsPresent Error]: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifies MVVM integration and ViewModel connectivity.
        /// </summary>
        private void VerifyMVVMIntegration()
        {
            Debug.WriteLine("=== MVVM Integration Verification ===");
            
            try
            {
                if (DataContext is AdminMainViewModel viewModel)
                {
                    Debug.WriteLine("? DataContext properly set to AdminMainViewModel");
                    Debug.WriteLine($"? ActiveEmployees collection initialized: {viewModel.ActiveEmployees != null}");
                    Debug.WriteLine($"? SystemAlerts collection initialized: {viewModel.SystemAlerts != null}");
                    Debug.WriteLine($"? RecentActivity collection initialized: {viewModel.RecentActivity != null}");
                    
                    // Verify key properties
                    Debug.WriteLine($"? ClockedInCount: {viewModel.ClockedInCount}");
                    Debug.WriteLine($"? TodayEntries: {viewModel.TodayEntries}");
                    Debug.WriteLine($"? SystemHealthIcon: '{viewModel.SystemHealthIcon}'");
                    Debug.WriteLine($"? TabletId: '{viewModel.TabletId}'");
                    Debug.WriteLine($"? TabletLocation: '{viewModel.TabletLocation}'");
                    Debug.WriteLine($"? AppVersion: '{viewModel.AppVersion}'");
                    
                    // Check command availability
                    Debug.WriteLine($"? Commands initialized:");
                    Debug.WriteLine($"  - BackToMainCommand: {viewModel.BackToMainCommand != null}");
                    Debug.WriteLine($"  - RefreshDataCommand: {viewModel.RefreshDataCommand != null}");
                    Debug.WriteLine($"  - LogoutCommand: {viewModel.LogoutCommand != null}");
                    Debug.WriteLine($"  - SettingsCommand: {viewModel.SettingsCommand != null}");
                    Debug.WriteLine($"  - DashboardCommand: {viewModel.DashboardCommand != null}");
                    Debug.WriteLine($"  - EmployeeManagementCommand: {viewModel.EmployeeManagementCommand != null}");
                }
                else
                {
                    Debug.WriteLine("? DataContext not properly set to AdminMainViewModel");
                    Debug.WriteLine($"? Actual DataContext type: {DataContext?.GetType().Name ?? "null"}");
                }
                
                Debug.WriteLine("=== MVVM Integration Verification Complete ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VerifyMVVMIntegration Error]: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifies data binding connections between UI elements and ViewModel properties.
        /// </summary>
        private void VerifyDataBindings()
        {
            Debug.WriteLine("=== Data Binding Verification ===");
            
            try
            {
                var textBlocks = FindVisualChildren<TextBlock>(this).ToList();
                var boundTextBlocks = 0;
                
                foreach (var textBlock in textBlocks)
                {
                    var binding = textBlock.GetBindingExpression(TextBlock.TextProperty);
                    if (binding != null)
                    {
                        boundTextBlocks++;
                        Debug.WriteLine($"? TextBlock bound to: {binding.ParentBinding.Path.Path}");
                    }
                }
                
                Debug.WriteLine($"? Found {boundTextBlocks} data-bound TextBlocks out of {textBlocks.Count} total");
                
                // Check for specific important bindings
                CheckSpecificBinding("ClockedInCount");
                CheckSpecificBinding("TodayEntries");
                CheckSpecificBinding("SystemHealthIcon");
                CheckSpecificBinding("SystemHealthColor");
                
                Debug.WriteLine("=== Data Binding Verification Complete ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VerifyDataBindings Error]: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifies command binding connections between UI elements and ViewModel commands.
        /// </summary>
        private void VerifyCommandBindings()
        {
            Debug.WriteLine("=== Command Binding Verification ===");
            
            try
            {
                var buttons = FindVisualChildren<Button>(this).ToList();
                var boundButtons = 0;
                
                foreach (var button in buttons)
                {
                    var commandBinding = button.GetBindingExpression(Button.CommandProperty);
                    if (commandBinding != null)
                    {
                        boundButtons++;
                        var buttonContent = GetButtonDisplayText(button);
                        Debug.WriteLine($"? Button '{buttonContent}' bound to command: {commandBinding.ParentBinding.Path.Path}");
                    }
                    else
                    {
                        var buttonContent = GetButtonDisplayText(button);
                        Debug.WriteLine($"? Button '{buttonContent}' has no command binding");
                    }
                }
                
                Debug.WriteLine($"? Found {boundButtons} command-bound buttons out of {buttons.Count} total");
                
                // Check for specific important command bindings
                CheckSpecificCommandBinding("BackToMainCommand");
                CheckSpecificCommandBinding("RefreshDataCommand");
                CheckSpecificCommandBinding("LogoutCommand");
                CheckSpecificCommandBinding("SettingsCommand");
                
                Debug.WriteLine("=== Command Binding Verification Complete ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VerifyCommandBindings Error]: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks for a specific data binding by property name.
        /// </summary>
        /// <param name="propertyName">The name of the property to check for.</param>
        private void CheckSpecificBinding(string propertyName)
        {
            try
            {
                var textBlocks = FindVisualChildren<TextBlock>(this);
                var found = textBlocks.Any(tb =>
                {
                    var binding = tb.GetBindingExpression(TextBlock.TextProperty);
                    return binding?.ParentBinding.Path.Path == propertyName;
                });
                
                Debug.WriteLine(found ? $"? Found binding for {propertyName}" : $"? Missing binding for {propertyName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CheckSpecificBinding Error for {propertyName}]: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks for a specific command binding by command name.
        /// </summary>
        /// <param name="commandName">The name of the command to check for.</param>
        private void CheckSpecificCommandBinding(string commandName)
        {
            try
            {
                var buttons = FindVisualChildren<Button>(this);
                var found = buttons.Any(btn =>
                {
                    var binding = btn.GetBindingExpression(Button.CommandProperty);
                    return binding?.ParentBinding.Path.Path == commandName;
                });
                
                Debug.WriteLine(found ? $"? Found command binding for {commandName}" : $"? Missing command binding for {commandName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CheckSpecificCommandBinding Error for {commandName}]: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets display text for a button for diagnostic purposes.
        /// </summary>
        /// <param name="button">The button to get text from.</param>
        /// <returns>A string representation of the button's content.</returns>
        private static string GetButtonDisplayText(Button button)
        {
            try
            {
                if (button.Content is string text)
                    return text;
                
                if (button.Content is StackPanel stackPanel)
                {
                    var textBlocks = FindVisualChildren<TextBlock>(stackPanel);
                    var texts = textBlocks.Select(tb => tb.Text).Where(t => !string.IsNullOrEmpty(t));
                    return string.Join(" ", texts);
                }
                
                if (button.Content is TextBlock textBlock)
                    return textBlock.Text ?? "Unknown";
                
                return button.Content?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Helper method to find the first visual child of a specific type.
        /// </summary>
        /// <typeparam name="T">The type of visual child to find.</typeparam>
        /// <param name="parent">The parent visual element to search within.</param>
        /// <returns>The first visual child of the specified type, or null if not found.</returns>
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            return FindVisualChildren<T>(parent).FirstOrDefault();
        }

        /// <summary>
        /// Helper method to find visual children of a specific type in the visual tree.
        /// Used for comprehensive UI verification and diagnostic purposes.
        /// </summary>
        /// <typeparam name="T">The type of visual children to find.</typeparam>
        /// <param name="parent">The parent visual element to search within.</param>
        /// <returns>Collection of visual children of the specified type.</returns>
        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
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
            Debug.WriteLine("=== AdminMainWindow OnClosed Begin ===");
            
            try
            {
                // Additional cleanup if needed
                Debug.WriteLine("AdminMainWindow: OnClosed cleanup completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminMainWindow OnClosed Error]: {ex.Message}");
            }
            finally
            {
                Debug.WriteLine("=== AdminMainWindow OnClosed Complete ===");
                base.OnClosed(e);
            }
        }
    }
}
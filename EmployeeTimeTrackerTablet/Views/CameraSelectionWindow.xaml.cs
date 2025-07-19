using System;
using System.Diagnostics;
using System.Windows;
using EmployeeTimeTrackerTablet.ViewModels;
using EmployeeTimeTrackerTablet.Models;

namespace EmployeeTimeTrackerTablet.Views
{
    /// <summary>
    /// Interaction logic for CameraSelectionWindow.xaml
    /// Administrative window for selecting camera devices in the Employee Time Tracker system.
    /// Provides a user-friendly interface for camera management and selection.
    /// </summary>
    public partial class CameraSelectionWindow : Window
    {
        private readonly CameraSelectionViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the CameraSelectionWindow.
        /// Sets up the window with its ViewModel and configures event handlers.
        /// </summary>
        public CameraSelectionWindow()
        {
            try
            {
                Debug.WriteLine("CameraSelectionWindow: Initializing window...");
                
                InitializeComponent();
                
                // Create and set the ViewModel
                _viewModel = new CameraSelectionViewModel();
                DataContext = _viewModel;
                
                // Subscribe to ViewModel property changes for dialog management
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                
                Debug.WriteLine("CameraSelectionWindow: Window initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CameraSelectionWindow: Error during initialization - {ex.Message}");
                Debug.WriteLine($"CameraSelectionWindow: Stack trace - {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Constructor that accepts a parent window for proper modal behavior.
        /// </summary>
        /// <param name="owner">The parent window that owns this dialog.</param>
        public CameraSelectionWindow(Window owner) : this()
        {
            try
            {
                if (owner != null)
                {
                    Owner = owner;
                    Debug.WriteLine("CameraSelectionWindow: Parent window set for modal behavior");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CameraSelectionWindow: Error setting parent window - {ex.Message}");
            }
        }

        /// <summary>
        /// Handles property changes from the ViewModel, particularly for dialog result management.
        /// Automatically closes the dialog when the selection is confirmed or cancelled.
        /// </summary>
        /// <param name="sender">The ViewModel that raised the event.</param>
        /// <param name="e">Event arguments containing the property change information.</param>
        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == nameof(CameraSelectionViewModel.DialogResult))
                {
                    // The ViewModel has set the dialog result, so we should close the window
                    Debug.WriteLine($"CameraSelectionWindow: Dialog result changed to {_viewModel.DialogResult}");
                    
                    // Set the WPF DialogResult based on the ViewModel's DialogResult
                    DialogResult = _viewModel.DialogResult;
                    
                    Debug.WriteLine("CameraSelectionWindow: Closing dialog window");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CameraSelectionWindow: Error handling property change - {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the selected camera device from the ViewModel.
        /// This method should be called after the dialog is closed with a positive result.
        /// </summary>
        /// <returns>The selected CameraDevice, or null if no selection was made.</returns>
        public CameraDevice? GetSelectedCamera()
        {
            try
            {
                var selectedCamera = _viewModel.GetSelectedCamera();
                Debug.WriteLine($"CameraSelectionWindow: Returning selected camera - {selectedCamera?.Name ?? "None"}");
                return selectedCamera;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CameraSelectionWindow: Error getting selected camera - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Handles the window closing event to ensure proper cleanup.
        /// </summary>
        /// <param name="e">Event arguments for the closing event.</param>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                Debug.WriteLine("CameraSelectionWindow: Window closing...");
                
                // Unsubscribe from ViewModel events to prevent memory leaks
                if (_viewModel != null)
                {
                    _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                }
                
                base.OnClosing(e);
                Debug.WriteLine("CameraSelectionWindow: Window closed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CameraSelectionWindow: Error during window closing - {ex.Message}");
                base.OnClosing(e);
            }
        }

        /// <summary>
        /// Static method to show the camera selection dialog and return the selected camera.
        /// This provides a convenient way to use the dialog from other parts of the application.
        /// </summary>
        /// <param name="parentWindow">The parent window for modal behavior.</param>
        /// <returns>The selected CameraDevice, or null if cancelled or no selection was made.</returns>
        public static CameraDevice? ShowCameraSelection(Window? parentWindow = null)
        {
            try
            {
                Debug.WriteLine("CameraSelectionWindow: Showing camera selection dialog...");
                
                var dialog = parentWindow != null 
                    ? new CameraSelectionWindow(parentWindow) 
                    : new CameraSelectionWindow();
                
                var result = dialog.ShowDialog();
                
                if (result == true)
                {
                    var selectedCamera = dialog.GetSelectedCamera();
                    Debug.WriteLine($"CameraSelectionWindow: Dialog completed with selection - {selectedCamera?.Name ?? "None"}");
                    return selectedCamera;
                }
                else
                {
                    Debug.WriteLine("CameraSelectionWindow: Dialog cancelled or no selection made");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CameraSelectionWindow: Error showing camera selection dialog - {ex.Message}");
                Debug.WriteLine($"CameraSelectionWindow: Stack trace - {ex.StackTrace}");
                return null;
            }
        }
    }
}
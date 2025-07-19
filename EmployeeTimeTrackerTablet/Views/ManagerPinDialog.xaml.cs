using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using EmployeeTimeTrackerTablet.Services;

namespace EmployeeTimeTrackerTablet.Views
{
    /// <summary>
    /// Interaction logic for ManagerPinDialog.xaml
    /// Provides PIN-based authentication for manager time correction access.
    /// </summary>
    public partial class ManagerPinDialog : Window
    {
        private readonly ManagerAuthService _managerAuthService;
        private bool _isAuthenticating = false;

        /// <summary>
        /// Gets whether the authentication was successful.
        /// </summary>
        public bool IsAuthenticated { get; private set; } = false;

        /// <summary>
        /// Initializes a new instance of the ManagerPinDialog.
        /// </summary>
        /// <param name="managerAuthService">The manager authentication service.</param>
        public ManagerPinDialog(ManagerAuthService managerAuthService)
        {
            InitializeComponent();
            _managerAuthService = managerAuthService ?? throw new ArgumentNullException(nameof(managerAuthService));
            
            // Set focus to PIN PasswordBox when dialog opens
            Loaded += (s, e) => PinPasswordBox.Focus();
            
            System.Diagnostics.Debug.WriteLine("ManagerPinDialog: Dialog initialized");
        }

        /// <summary>
        /// Handles the PasswordChanged event for the PIN PasswordBox.
        /// Enables/disables the authenticate button based on PIN length.
        /// </summary>
        private void PinPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                var pin = PinPasswordBox.Password;
                
                // Enable authenticate button only when PIN has 4 digits
                AuthenticateButton.IsEnabled = pin.Length == 4 && !_isAuthenticating;
                
                // Clear error message when user starts typing
                if (ErrorMessage.Visibility == Visibility.Visible)
                {
                    ErrorMessage.Visibility = Visibility.Collapsed;
                }
                
                // Validate PIN format in real-time
                if (pin.Length > 0 && !_managerAuthService.ValidatePinFormat(pin))
                {
                    if (pin.Length == 4) // Only show error when PIN is complete
                    {
                        ShowErrorMessage("PIN must contain only digits");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ManagerPinDialog: Error in PinPasswordBox_PasswordChanged: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the KeyDown event for the PIN PasswordBox.
        /// Allows authentication via Enter key when PIN is complete.
        /// </summary>
        private async void PinPasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter && AuthenticateButton.IsEnabled)
                {
                    await AuthenticateAsync();
                }
                else if (e.Key == Key.Escape)
                {
                    CancelAuthentication();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ManagerPinDialog: Error in PinPasswordBox_KeyDown: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the Click event for the Authenticate button.
        /// </summary>
        private async void AuthenticateButton_Click(object sender, RoutedEventArgs e)
        {
            await AuthenticateAsync();
        }

        /// <summary>
        /// Handles the Click event for the Cancel button.
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CancelAuthentication();
        }

        /// <summary>
        /// Performs the authentication process.
        /// </summary>
        private async Task AuthenticateAsync()
        {
            try
            {
                if (_isAuthenticating)
                {
                    return; // Prevent multiple authentication attempts
                }

                var pin = PinPasswordBox.Password;
                
                if (string.IsNullOrWhiteSpace(pin))
                {
                    ShowErrorMessage("Please enter a PIN");
                    return;
                }

                if (!_managerAuthService.ValidatePinFormat(pin))
                {
                    ShowErrorMessage("PIN must be exactly 4 digits");
                    return;
                }

                _isAuthenticating = true;
                ShowLoadingState();
                
                System.Diagnostics.Debug.WriteLine($"ManagerPinDialog: Attempting authentication with PIN length: {pin.Length}");

                // Perform authentication
                var isAuthenticated = await _managerAuthService.AuthenticateAsync(pin);

                if (isAuthenticated)
                {
                    System.Diagnostics.Debug.WriteLine("ManagerPinDialog: Authentication successful");
                    IsAuthenticated = true;
                    ShowSuccessMessage("Authentication successful!");
                    
                    // Brief delay to show success message
                    await Task.Delay(1000);
                    
                    DialogResult = true;
                    Close();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ManagerPinDialog: Authentication failed");
                    ShowErrorMessage("Invalid PIN. Access denied.");
                    
                    // Clear PIN for retry
                    PinPasswordBox.Clear();
                    PinPasswordBox.Focus();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ManagerPinDialog: Authentication error: {ex.Message}");
                ShowErrorMessage("Authentication error. Please try again.");
            }
            finally
            {
                _isAuthenticating = false;
                HideLoadingState();
            }
        }

        /// <summary>
        /// Cancels the authentication process and closes the dialog.
        /// </summary>
        private void CancelAuthentication()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ManagerPinDialog: Authentication cancelled by user");
                IsAuthenticated = false;
                DialogResult = false;
                Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ManagerPinDialog: Error in CancelAuthentication: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows an error message to the user.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        private void ShowErrorMessage(string message)
        {
            try
            {
                ErrorMessage.Text = message;
                ErrorMessage.Visibility = Visibility.Visible;
                StatusMessage.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ManagerPinDialog: Error showing error message: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows a success message to the user.
        /// </summary>
        /// <param name="message">The success message to display.</param>
        private void ShowSuccessMessage(string message)
        {
            try
            {
                StatusMessage.Text = message;
                StatusMessage.Visibility = Visibility.Visible;
                ErrorMessage.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ManagerPinDialog: Error showing success message: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows the loading state during authentication.
        /// </summary>
        private void ShowLoadingState()
        {
            try
            {
                LoadingPanel.Visibility = Visibility.Visible;
                ErrorMessage.Visibility = Visibility.Collapsed;
                StatusMessage.Visibility = Visibility.Collapsed;
                AuthenticateButton.IsEnabled = false;
                CancelButton.IsEnabled = false;
                PinPasswordBox.IsEnabled = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ManagerPinDialog: Error showing loading state: {ex.Message}");
            }
        }

        /// <summary>
        /// Hides the loading state after authentication.
        /// </summary>
        private void HideLoadingState()
        {
            try
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
                AuthenticateButton.IsEnabled = PinPasswordBox.Password.Length == 4;
                CancelButton.IsEnabled = true;
                PinPasswordBox.IsEnabled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ManagerPinDialog: Error hiding loading state: {ex.Message}");
            }
        }
    }
}
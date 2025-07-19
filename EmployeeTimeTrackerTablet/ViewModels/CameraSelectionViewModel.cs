using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EmployeeTimeTrackerTablet.Models;
using EmployeeTimeTrackerTablet.Services;
using Windows.Devices.Enumeration;

namespace EmployeeTimeTrackerTablet.ViewModels
{
    /// <summary>
    /// ViewModel for the Camera Selection administrative window.
    /// Provides functionality for detecting, listing, and selecting camera devices.
    /// Uses modern WinRT APIs for comprehensive camera detection.
    /// </summary>
    public partial class CameraSelectionViewModel : ObservableObject
    {
        private readonly CameraSettingsService _cameraSettingsService;

        #region Observable Properties

        /// <summary>
        /// Collection of available camera devices detected in the system.
        /// Bound to the UI for displaying camera options to the administrator.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<CameraDevice> availableCameras = new();

        /// <summary>
        /// The currently selected camera device.
        /// This represents the admin's choice for the active camera.
        /// </summary>
        [ObservableProperty]
        private CameraDevice selectedCamera;

        /// <summary>
        /// Status message for displaying operation results and feedback to the user.
        /// Shows loading states, error messages, and success confirmations.
        /// </summary>
        [ObservableProperty]
        private string statusMessage = "Loading available cameras...";

        /// <summary>
        /// Indicates whether a camera detection operation is currently in progress.
        /// Used to control UI state and prevent multiple simultaneous operations.
        /// </summary>
        [ObservableProperty]
        private bool isLoading = true;

        /// <summary>
        /// Indicates whether the dialog has been completed with a valid selection.
        /// Used by the parent window to determine if the selection should be applied.
        /// </summary>
        [ObservableProperty]
        private bool dialogResult = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the CameraSelectionViewModel.
        /// Automatically begins camera detection upon creation.
        /// </summary>
        public CameraSelectionViewModel() : this(new CameraSettingsService())
        {
        }

        /// <summary>
        /// Initializes a new instance of the CameraSelectionViewModel with dependency injection.
        /// Automatically begins camera detection upon creation.
        /// </summary>
        /// <param name="cameraSettingsService">Service for managing camera settings persistence.</param>
        public CameraSelectionViewModel(CameraSettingsService cameraSettingsService)
        {
            Debug.WriteLine("CameraSelectionViewModel: Initializing...");
            
            _cameraSettingsService = cameraSettingsService ?? throw new ArgumentNullException(nameof(cameraSettingsService));
            
            // Initialize collections
            AvailableCameras = new ObservableCollection<CameraDevice>();
            
            // Start loading cameras asynchronously
            _ = LoadCamerasAsync();
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command for the OK button - confirms the camera selection.
        /// Sets the dialog result to true and prepares for window closure.
        /// Enhanced with camera selection persistence.
        /// </summary>
        [RelayCommand]
        private async Task SelectCamera()
        {
            try
            {
                Debug.WriteLine("CameraSelectionViewModel: SelectCamera command executed");
                
                if (SelectedCamera != null)
                {
                    Debug.WriteLine($"CameraSelectionViewModel: Camera selected - {SelectedCamera.Name} (ID: {SelectedCamera.Id})");
                    
                    // Save the selected camera to persistent storage
                    try
                    {
                        await _cameraSettingsService.SaveSelectedCameraAsync(SelectedCamera.Id);
                        Debug.WriteLine($"CameraSelectionViewModel: Camera selection saved to persistent storage");
                        
                        // Also save the display name for user reference
                        var settings = _cameraSettingsService.GetCurrentSettings();
                        settings.SelectedCameraName = SelectedCamera.DisplayName;
                        await _cameraSettingsService.SaveSettingsAsync(settings);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"CameraSelectionViewModel: Warning - Failed to save camera selection: {ex.Message}");
                        // Continue with selection even if saving fails
                    }
                    
                    StatusMessage = $"Selected: {SelectedCamera.Name}";
                    DialogResult = true;
                }
                else
                {
                    Debug.WriteLine("CameraSelectionViewModel: No camera selected");
                    StatusMessage = "Please select a camera before confirming.";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CameraSelectionViewModel: Error in SelectCamera - {ex.Message}");
                StatusMessage = "Error confirming camera selection.";
            }
        }

        /// <summary>
        /// Command for the Cancel button - cancels the camera selection operation.
        /// Sets the dialog result to false and prepares for window closure.
        /// </summary>
        [RelayCommand]
        private void CancelSelection()
        {
            try
            {
                Debug.WriteLine("CameraSelectionViewModel: CancelSelection command executed");
                StatusMessage = "Camera selection cancelled.";
                DialogResult = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CameraSelectionViewModel: Error in CancelSelection - {ex.Message}");
                StatusMessage = "Error cancelling selection.";
            }
        }

        /// <summary>
        /// Command to refresh the camera list - re-detects available cameras.
        /// Useful when cameras are plugged/unplugged while the dialog is open.
        /// </summary>
        [RelayCommand]
        private async Task RefreshCamerasAsync()
        {
            try
            {
                Debug.WriteLine("CameraSelectionViewModel: RefreshCameras command executed");
                StatusMessage = "Refreshing camera list...";
                IsLoading = true;
                
                await LoadCamerasAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CameraSelectionViewModel: Error in RefreshCameras - {ex.Message}");
                StatusMessage = "Error refreshing camera list.";
                IsLoading = false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads available camera devices using Windows.Devices.Enumeration APIs.
        /// This method provides comprehensive camera detection using modern WinRT APIs.
        /// </summary>
        private async Task LoadCamerasAsync()
        {
            try
            {
                Debug.WriteLine("CameraSelectionViewModel: Starting camera detection...");
                IsLoading = true;
                StatusMessage = "Detecting cameras...";

                // Clear existing cameras
                AvailableCameras.Clear();
                SelectedCamera = null;

                // Use Windows.Devices.Enumeration to find video capture devices
                var deviceInfoCollection = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                
                Debug.WriteLine($"CameraSelectionViewModel: Found {deviceInfoCollection.Count} video capture devices");

                if (deviceInfoCollection.Count == 0)
                {
                    // No cameras found
                    StatusMessage = "No cameras detected. Please ensure a camera is connected and enabled.";
                    Debug.WriteLine("CameraSelectionViewModel: No cameras found using DeviceInformation.FindAllAsync");
                    
                    // Add a placeholder entry to inform the user
                    AvailableCameras.Add(new CameraDevice
                    {
                        Id = "none",
                        Name = "No cameras detected",
                        Description = "Please connect a camera and refresh",
                        IsAvailable = false
                    });
                }
                else
                {
                    // Process detected cameras using enhanced creation method
                    foreach (var deviceInfo in deviceInfoCollection)
                    {
                        try
                        {
                            var cameraDevice = CreateCameraDevice(deviceInfo);
                            AvailableCameras.Add(cameraDevice);
                            Debug.WriteLine($"CameraSelectionViewModel: Added camera - {cameraDevice.DisplayName} (ID: {cameraDevice.Id})");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"CameraSelectionViewModel: Error processing camera device {deviceInfo.Id}: {ex.Message}");
                        }
                    }

                    // Enhanced camera selection logic with persistence support
                    await SelectPreviouslyChosenCameraAsync();

                    StatusMessage = $"Found {AvailableCameras.Count} camera(s). Select one and click 'Select Camera'.";
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"CameraSelectionViewModel: Camera access denied - {ex.Message}");
                StatusMessage = "Camera access denied. Please check privacy settings and try again.";
                
                // Add an informational entry
                AvailableCameras.Clear();
                AvailableCameras.Add(new CameraDevice
                {
                    Id = "access_denied",
                    Name = "Camera access denied",
                    Description = "Check Windows privacy settings for camera access",
                    IsAvailable = false
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CameraSelectionViewModel: Error loading cameras - {ex.Message}");
                Debug.WriteLine($"CameraSelectionViewModel: Exception stack trace - {ex.StackTrace}");
                StatusMessage = "Error detecting cameras. Please try refreshing or check system settings.";
                
                // Add an error entry
                AvailableCameras.Clear();
                AvailableCameras.Add(new CameraDevice
                {
                    Id = "error",
                    Name = "Error detecting cameras",
                    Description = ex.Message,
                    IsAvailable = false
                });
            }
            finally
            {
                IsLoading = false;
                Debug.WriteLine("CameraSelectionViewModel: Camera detection completed");
            }
        }

        /// <summary>
        /// Selects the previously chosen camera if available, otherwise selects the first available camera.
        /// This method implements the camera selection persistence logic.
        /// </summary>
        private async Task SelectPreviouslyChosenCameraAsync()
        {
            try
            {
                Debug.WriteLine("CameraSelectionViewModel: Attempting to select previously chosen camera...");
                
                // Load current settings
                var settings = await _cameraSettingsService.LoadSettingsAsync();
                var savedCameraId = settings.SelectedCameraId;
                
                CameraDevice cameraToSelect = null;
                
                if (!string.IsNullOrWhiteSpace(savedCameraId))
                {
                    Debug.WriteLine($"CameraSelectionViewModel: Looking for previously selected camera ID: {savedCameraId}");
                    
                    // Try to find the previously selected camera
                    cameraToSelect = AvailableCameras.FirstOrDefault(c => 
                        string.Equals(c.Id, savedCameraId, StringComparison.OrdinalIgnoreCase) && c.IsAvailable);
                    
                    if (cameraToSelect != null)
                    {
                        Debug.WriteLine($"CameraSelectionViewModel: Found and selected previously chosen camera: {cameraToSelect.DisplayName}");
                        StatusMessage = $"Found {AvailableCameras.Count} camera(s). Previously selected camera '{cameraToSelect.DisplayName}' is active.";
                    }
                    else
                    {
                        Debug.WriteLine($"CameraSelectionViewModel: Previously selected camera not found or unavailable: {savedCameraId}");
                        
                        // Clear the invalid camera selection
                        try
                        {
                            await _cameraSettingsService.ClearSelectedCameraAsync();
                            Debug.WriteLine("CameraSelectionViewModel: Cleared invalid camera selection from settings");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"CameraSelectionViewModel: Warning - Failed to clear invalid camera selection: {ex.Message}");
                        }
                    }
                }
                
                // If no previously selected camera or it's not available, select the first available camera
                if (cameraToSelect == null)
                {
                    cameraToSelect = AvailableCameras.FirstOrDefault(c => c.IsAvailable);
                    
                    if (cameraToSelect != null)
                    {
                        Debug.WriteLine($"CameraSelectionViewModel: Auto-selected first available camera: {cameraToSelect.DisplayName}");
                        if (!string.IsNullOrWhiteSpace(savedCameraId))
                        {
                            StatusMessage = $"Found {AvailableCameras.Count} camera(s). Previously selected camera not available - using '{cameraToSelect.DisplayName}'.";
                        }
                    }
                    else
                    {
                        Debug.WriteLine("CameraSelectionViewModel: No available cameras found for selection");
                    }
                }
                
                // Set the selected camera
                SelectedCamera = cameraToSelect;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CameraSelectionViewModel: Error in SelectPreviouslyChosenCameraAsync: {ex.Message}");
                
                // Fallback to first available camera if there's an error
                var fallbackCamera = AvailableCameras.FirstOrDefault(c => c.IsAvailable);
                if (fallbackCamera != null)
                {
                    SelectedCamera = fallbackCamera;
                    Debug.WriteLine($"CameraSelectionViewModel: Fallback - selected first available camera: {fallbackCamera.DisplayName}");
                }
            }
        }

        /// <summary>
        /// Creates a descriptive string for a camera device based on its properties.
        /// Enhanced to extract manufacturer and connection type information.
        /// </summary>
        /// <param name="deviceInfo">The DeviceInformation object containing camera details.</param>
        /// <returns>A formatted description string for the camera.</returns>
        private static string CreateCameraDescription(DeviceInformation deviceInfo)
        {
            try
            {
                var parts = new List<string>();
                
                // Add basic device class information
                if (deviceInfo.Properties.ContainsKey("System.Devices.InterfaceClassGuid"))
                {
                    parts.Add("Video Capture Device");
                }
                
                // Try to extract connection information
                if (deviceInfo.Id.Contains("USB", StringComparison.OrdinalIgnoreCase))
                {
                    parts.Add("USB Connection");
                }
                else if (deviceInfo.Id.Contains("ROOT", StringComparison.OrdinalIgnoreCase))
                {
                    parts.Add("Built-in Device");
                }
                
                // Add enabled/disabled status
                if (!deviceInfo.IsEnabled)
                {
                    parts.Add("Disabled");
                }
                
                return parts.Count > 0 ? string.Join(" - ", parts) : "Camera Device";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CameraSelectionViewModel: Error creating camera description - {ex.Message}");
                return "Camera Device";
            }
        }

        /// <summary>
        /// Enhanced method to create a CameraDevice with detailed information.
        /// Extracts manufacturer, connection type, and other details from DeviceInformation.
        /// </summary>
        /// <param name="deviceInfo">The DeviceInformation object containing camera details.</param>
        /// <returns>A fully populated CameraDevice instance.</returns>
        private static CameraDevice CreateCameraDevice(DeviceInformation deviceInfo)
        {
            try
            {
                var cameraDevice = new CameraDevice
                {
                    Id = deviceInfo.Id,
                    Name = !string.IsNullOrEmpty(deviceInfo.Name) ? deviceInfo.Name : "Unknown Camera",
                    Description = CreateCameraDescription(deviceInfo),
                    IsAvailable = deviceInfo.IsEnabled
                };

                // Extract manufacturer information from device name
                ExtractManufacturerInfo(cameraDevice, deviceInfo);
                
                // Extract connection type from device ID
                ExtractConnectionType(cameraDevice, deviceInfo);

                return cameraDevice;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CameraSelectionViewModel: Error creating camera device - {ex.Message}");
                return new CameraDevice
                {
                    Id = deviceInfo?.Id ?? "unknown",
                    Name = "Unknown Camera",
                    Description = "Error retrieving device information",
                    IsAvailable = false
                };
            }
        }

        /// <summary>
        /// Extracts manufacturer information from the device name or properties.
        /// </summary>
        /// <param name="cameraDevice">The CameraDevice to populate.</param>
        /// <param name="deviceInfo">The DeviceInformation source.</param>
        private static void ExtractManufacturerInfo(CameraDevice cameraDevice, DeviceInformation deviceInfo)
        {
            try
            {
                var name = deviceInfo.Name?.ToLowerInvariant() ?? "";
                
                // Common camera manufacturers
                var manufacturers = new Dictionary<string, string>
                {
                    { "logitech", "Logitech" },
                    { "microsoft", "Microsoft" },
                    { "hp", "HP" },
                    { "dell", "Dell" },
                    { "lenovo", "Lenovo" },
                    { "asus", "ASUS" },
                    { "acer", "Acer" },
                    { "sony", "Sony" },
                    { "canon", "Canon" },
                    { "creative", "Creative" },
                    { "razer", "Razer" },
                    { "elgato", "Elgato" },
                    { "webcam", "Generic" }
                };

                foreach (var kvp in manufacturers)
                {
                    if (name.Contains(kvp.Key))
                    {
                        cameraDevice.Manufacturer = kvp.Value;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CameraSelectionViewModel: Error extracting manufacturer info - {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts connection type information from the device ID.
        /// </summary>
        /// <param name="cameraDevice">The CameraDevice to populate.</param>
        /// <param name="deviceInfo">The DeviceInformation source.</param>
        private static void ExtractConnectionType(CameraDevice cameraDevice, DeviceInformation deviceInfo)
        {
            try
            {
                var deviceId = deviceInfo.Id?.ToUpperInvariant() ?? "";
                
                if (deviceId.Contains("USB"))
                {
                    cameraDevice.ConnectionType = "USB";
                }
                else if (deviceId.Contains("ROOT") || deviceId.Contains("ACPI"))
                {
                    cameraDevice.ConnectionType = "Built-in";
                }
                else if (deviceId.Contains("NETWORK") || deviceId.Contains("IP"))
                {
                    cameraDevice.ConnectionType = "Network";
                }
                else
                {
                    cameraDevice.ConnectionType = "Other";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CameraSelectionViewModel: Error extracting connection type - {ex.Message}");
                cameraDevice.ConnectionType = "Unknown";
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the currently selected camera device for external access.
        /// Used by the parent window to retrieve the admin's camera selection.
        /// </summary>
        /// <returns>The selected CameraDevice, or null if no selection was made.</returns>
        public CameraDevice GetSelectedCamera()
        {
            return SelectedCamera;
        }

        /// <summary>
        /// Gets a value indicating whether the dialog was completed successfully.
        /// Used by the parent window to determine if the selection should be applied.
        /// </summary>
        /// <returns>True if a camera was selected and confirmed; otherwise, false.</returns>
        public bool GetDialogResult()
        {
            return DialogResult;
        }

        #endregion
    }
}
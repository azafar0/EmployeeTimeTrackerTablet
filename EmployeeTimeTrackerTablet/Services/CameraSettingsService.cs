using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace EmployeeTimeTrackerTablet.Services
{
    /// <summary>
    /// Service for persisting and loading camera configuration settings.
    /// Provides camera selection persistence across application sessions.
    /// </summary>
    public class CameraSettingsService
    {
        private readonly string _settingsFilePath;
        private CameraSettings _currentSettings;

        /// <summary>
        /// Initializes a new instance of the CameraSettingsService.
        /// </summary>
        public CameraSettingsService()
        {
            // Create settings directory in user's AppData
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appDirectory = Path.Combine(appDataPath, "EmployeeTimeTracker");
            
            if (!Directory.Exists(appDirectory))
            {
                Directory.CreateDirectory(appDirectory);
            }
            
            _settingsFilePath = Path.Combine(appDirectory, "camera-settings.json");
            _currentSettings = new CameraSettings();
            
            Debug.WriteLine($"CameraSettingsService: Settings file path: {_settingsFilePath}");
        }

        /// <summary>
        /// Loads the camera settings from storage.
        /// </summary>
        /// <returns>A task containing the loaded camera settings.</returns>
        public async Task<CameraSettings> LoadSettingsAsync()
        {
            try
            {
                Debug.WriteLine("CameraSettingsService: Loading camera settings...");
                
                if (!File.Exists(_settingsFilePath))
                {
                    Debug.WriteLine("CameraSettingsService: Settings file does not exist, returning default settings");
                    _currentSettings = new CameraSettings();
                    return _currentSettings;
                }

                var json = await File.ReadAllTextAsync(_settingsFilePath);
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    Debug.WriteLine("CameraSettingsService: Settings file is empty, returning default settings");
                    _currentSettings = new CameraSettings();
                    return _currentSettings;
                }

                var settings = JsonSerializer.Deserialize<CameraSettings>(json);
                _currentSettings = settings ?? new CameraSettings();
                
                Debug.WriteLine($"CameraSettingsService: Loaded settings - Selected Camera ID: {_currentSettings.SelectedCameraId ?? "None"}");
                return _currentSettings;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CameraSettingsService: Error loading settings: {ex.Message}");
                _currentSettings = new CameraSettings();
                return _currentSettings;
            }
        }

        /// <summary>
        /// Saves the camera settings to storage.
        /// </summary>
        /// <param name="settings">The camera settings to save.</param>
        /// <returns>A task indicating completion.</returns>
        public async Task SaveSettingsAsync(CameraSettings settings)
        {
            try
            {
                Debug.WriteLine($"CameraSettingsService: Saving camera settings - Camera ID: {settings.SelectedCameraId ?? "None"}");
                
                _currentSettings = settings;
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                var json = JsonSerializer.Serialize(settings, options);
                await File.WriteAllTextAsync(_settingsFilePath, json);
                
                Debug.WriteLine("CameraSettingsService: Settings saved successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CameraSettingsService: Error saving settings: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Saves the selected camera ID.
        /// </summary>
        /// <param name="cameraId">The ID of the selected camera device.</param>
        /// <returns>A task indicating completion.</returns>
        public async Task SaveSelectedCameraAsync(string cameraId)
        {
            try
            {
                Debug.WriteLine($"CameraSettingsService: Saving selected camera ID: {cameraId}");
                
                _currentSettings.SelectedCameraId = cameraId;
                _currentSettings.LastUpdated = DateTime.Now;
                
                await SaveSettingsAsync(_currentSettings);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CameraSettingsService: Error saving selected camera: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the currently selected camera ID.
        /// </summary>
        /// <returns>The selected camera ID, or null if none is selected.</returns>
        public string? GetSelectedCameraId()
        {
            return _currentSettings.SelectedCameraId;
        }

        /// <summary>
        /// Checks if a camera selection has been persisted.
        /// </summary>
        /// <returns>True if a camera has been selected and saved, false otherwise.</returns>
        public bool HasSelectedCamera()
        {
            return !string.IsNullOrWhiteSpace(_currentSettings.SelectedCameraId);
        }

        /// <summary>
        /// Clears the selected camera setting.
        /// </summary>
        /// <returns>A task indicating completion.</returns>
        public async Task ClearSelectedCameraAsync()
        {
            try
            {
                Debug.WriteLine("CameraSettingsService: Clearing selected camera");
                
                _currentSettings.SelectedCameraId = null;
                _currentSettings.LastUpdated = DateTime.Now;
                
                await SaveSettingsAsync(_currentSettings);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CameraSettingsService: Error clearing selected camera: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the full current settings object.
        /// </summary>
        /// <returns>The current camera settings.</returns>
        public CameraSettings GetCurrentSettings()
        {
            return _currentSettings;
        }
    }

    /// <summary>
    /// Data model for camera configuration settings.
    /// </summary>
    public class CameraSettings
    {
        /// <summary>
        /// The ID of the selected camera device.
        /// </summary>
        public string? SelectedCameraId { get; set; }

        /// <summary>
        /// The display name of the selected camera (for user reference).
        /// </summary>
        public string? SelectedCameraName { get; set; }

        /// <summary>
        /// The date and time when the settings were last updated.
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        /// <summary>
        /// Version number for settings format compatibility.
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Additional settings for future expansion.
        /// </summary>
        public bool AutoSelectFirstCamera { get; set; } = true;

        /// <summary>
        /// Whether to show camera selection dialog on startup if no camera is selected.
        /// </summary>
        public bool ShowSelectionDialogOnStartup { get; set; } = false;
    }
}
using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Win32;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Devices.Enumeration;

namespace EmployeeTimeTrackerTablet.Services
{
    /// <summary>
    /// Event arguments for camera device change notifications.
    /// PHASE 5.3: Dynamic camera monitoring support.
    /// </summary>
    public class CameraDeviceChangedEventArgs : EventArgs
    {
        public string DeviceId { get; }
        public string DeviceName { get; }
        public CameraChangeType ChangeType { get; }
        public bool IsPreferredCamera { get; }

        public CameraDeviceChangedEventArgs(string deviceId, string deviceName, CameraChangeType changeType, bool isPreferredCamera = false)
        {
            DeviceId = deviceId ?? string.Empty;
            DeviceName = deviceName ?? string.Empty;
            ChangeType = changeType;
            IsPreferredCamera = isPreferredCamera;
        }
    }

    /// <summary>
    /// Types of camera device changes.
    /// PHASE 5.3: Dynamic camera monitoring support.
    /// </summary>
    public enum CameraChangeType
    {
        Added,
        Removed,
        Updated
    }

    /// <summary>
    /// Provides camera integration services for capturing employee photos during clock-in and clock-out operations.
    /// This service abstracts camera functionality and file management for the time tracking system.
    /// Enhanced with comprehensive camera detection and diagnostic capabilities.
    /// Enhanced with camera selection persistence support.
    /// Enhanced with actual MediaCapture integration for real camera usage.
    /// PHASE 5.2: Complete implementation with preferred camera device selection.
    /// PHASE 5.3: Dynamic camera monitoring with DeviceWatcher integration.
    /// </summary>
    public class PhotoCaptureService : IDisposable
    {
        private readonly string _photoDirectory;
        private bool _cameraAvailable;
        private readonly CameraSettingsService _cameraSettingsService;
        private string? _preferredCameraId;
        private MediaCapture? _mediaCapture;
        private bool _mediaCaptureInitialized = false;

        // PHASE 5.3: Dynamic camera monitoring fields
        private DeviceWatcher? _deviceWatcher;
        private bool _isMonitoring = false;

        // PHASE 5.3: Event for notifying about camera device changes
        public event EventHandler<CameraDeviceChangedEventArgs>? CameraDeviceChanged;

        public PhotoCaptureService() : this(new CameraSettingsService()) { }

        public PhotoCaptureService(CameraSettingsService cameraSettingsService)
        {
            _cameraSettingsService = cameraSettingsService ?? throw new ArgumentNullException(nameof(cameraSettingsService));
            
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _photoDirectory = Path.Combine(documentsPath, "EmployeeTimeTracker", "Photos");
            
            if (!Directory.Exists(_photoDirectory))
            {
                Directory.CreateDirectory(_photoDirectory);
                Debug.WriteLine($"PhotoCaptureService: Created photo directory: {_photoDirectory}");
            }

            Debug.WriteLine($"PhotoCaptureService: Initialized with photo directory: {_photoDirectory}");
            _ = InitializeCameraSettingsAsync();
            
            // PHASE 5.3: Start dynamic camera monitoring
            StartCameraMonitoring();
        }

        public async Task<string?> CapturePhotoAsync(int employeeId, string photoType = "photo")
        {
            try
            {
                Debug.WriteLine($"PhotoCaptureService: Starting photo capture for Employee ID: {employeeId}, Type: {photoType}");
                
                // PHASE 5.4: Enhanced camera availability validation
                bool cameraAvailable = await ValidateCameraAvailabilityAsync();
                Debug.WriteLine($"PhotoCaptureService: Camera availability check result: {cameraAvailable}");
                
                if (!cameraAvailable)
                {
                    Debug.WriteLine("PhotoCaptureService: No camera available, using simulation mode");
                    return await CaptureSimulatedPhotoAsync(employeeId, photoType);
                }

                // PHASE 5.4: Enhanced camera initialization with detailed logging
                bool cameraInitialized = await InitializeCameraAsync();
                Debug.WriteLine($"PhotoCaptureService: Camera initialization result: {cameraInitialized}");
                
                if (!cameraInitialized)
                {
                    Debug.WriteLine("PhotoCaptureService: Failed to initialize camera, falling back to simulation");
                    return await CaptureSimulatedPhotoAsync(employeeId, photoType);
                }

                // PHASE 5.4: Attempt real photo capture
                Debug.WriteLine("PhotoCaptureService: Attempting to capture real photo from camera...");
                byte[]? photoData = await CaptureImageBytesAsync();
                
                if (photoData == null || photoData.Length == 0)
                {
                    Debug.WriteLine("PhotoCaptureService: Failed to capture photo data from camera, using simulation");
                    return await CaptureSimulatedPhotoAsync(employeeId, photoType);
                }

                Debug.WriteLine($"PhotoCaptureService: Successfully captured {photoData.Length} bytes from camera");

                // PHASE 5.4: Process real photo data
                byte[] processedPhoto = await CompressPhotoAsync(photoData);
                if (processedPhoto == null || processedPhoto.Length == 0)
                {
                    Debug.WriteLine("PhotoCaptureService: Photo compression failed, using original photo data");
                    processedPhoto = photoData;
                }

                Debug.WriteLine($"PhotoCaptureService: Photo processed, final size: {processedPhoto.Length} bytes");

                string fileName = GeneratePhotoFileName(employeeId, photoType);
                string? savedPath = await SavePhotoAsync(processedPhoto, fileName);

                if (savedPath != null)
                {
                    Debug.WriteLine($"PhotoCaptureService: REAL PHOTO captured and saved successfully to: {savedPath}");
                }
                else
                {
                    Debug.WriteLine("PhotoCaptureService: Failed to save real photo, falling back to simulation");
                    return await CaptureSimulatedPhotoAsync(employeeId, photoType);
                }

                return savedPath;
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Camera permission denied: {ex.Message}");
                return await CaptureSimulatedPhotoAsync(employeeId, photoType);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error capturing photo for Employee ID {employeeId}: {ex.Message}");
                Debug.WriteLine($"PhotoCaptureService: Exception details: {ex.StackTrace}");
                return await CaptureSimulatedPhotoAsync(employeeId, photoType);
            }
            finally
            {
                await ReleaseCameraAsync();
            }
        }

        public async Task<string?> CapturePhotoAsync(int employeeId) => await CapturePhotoAsync(employeeId, "photo");

        public async Task SetPreferredCameraAsync(string cameraId)
        {
            try
            {
                Debug.WriteLine($"PhotoCaptureService: Setting preferred camera to: {cameraId}");
                _preferredCameraId = cameraId;
                await _cameraSettingsService.SaveSelectedCameraAsync(cameraId);
                
                bool initSuccess = await InitializeCameraAsync();
                if (initSuccess)
                {
                    Debug.WriteLine("PhotoCaptureService: Preferred camera updated and applied successfully");
                }
                else
                {
                    Debug.WriteLine("PhotoCaptureService: Warning - Preferred camera saved but initialization failed");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error setting preferred camera: {ex.Message}");
                throw;
            }
        }

        public string? GetPreferredCameraId() => _preferredCameraId;

        public async Task<bool> ValidatePreferredCameraAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_preferredCameraId))
                    return false;

                var deviceInfo = await DeviceInformation.CreateFromIdAsync(_preferredCameraId);
                return deviceInfo != null && deviceInfo.IsEnabled;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error validating preferred camera: {ex.Message}");
                return false;
            }
        }

        public async Task<DeviceInformation?> GetPreferredCameraInfoAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_preferredCameraId))
                    return null;
                return await DeviceInformation.CreateFromIdAsync(_preferredCameraId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error getting preferred camera info: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> ValidateCameraAvailabilityAsync()
        {
            try
            {
                Debug.WriteLine("PhotoCaptureService: Validating camera availability...");
                
                // PHASE 5.4: Enhanced camera validation for real photo capture
                // Check if we have a preferred camera and if it's actually available
                if (!string.IsNullOrWhiteSpace(_preferredCameraId))
                {
                    Debug.WriteLine($"PhotoCaptureService: Checking preferred camera availability: {_preferredCameraId}");
                    try
                    {
                        var deviceInfo = await DeviceInformation.CreateFromIdAsync(_preferredCameraId);
                        if (deviceInfo != null && deviceInfo.IsEnabled)
                        {
                            Debug.WriteLine($"PhotoCaptureService: Preferred camera is available: {deviceInfo.Name}");
                            _cameraAvailable = true;
                            return true;
                        }
                        else
                        {
                            Debug.WriteLine("PhotoCaptureService: Preferred camera is not available or disabled");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"PhotoCaptureService: Error checking preferred camera: {ex.Message}");
                    }
                }
                
                // Check for any available video capture devices
                var videoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                var availableDevices = videoDevices.Where(d => d.IsEnabled).ToList();
                
                Debug.WriteLine($"PhotoCaptureService: Found {availableDevices.Count} available video capture devices");
                
                if (availableDevices.Count > 0)
                {
                    foreach (var device in availableDevices)
                    {
                        Debug.WriteLine($"PhotoCaptureService: Available camera - {device.Name} (ID: {device.Id})");
                    }
                    _cameraAvailable = true;
                    return true;
                }
                
                // Fallback to registry-based detection if no devices found via WinRT API
                Debug.WriteLine("PhotoCaptureService: No WinRT devices found, checking registry...");
                bool registryDetection = CheckCameraDevicesInRegistry();
                bool privacySettings = CheckCameraPrivacySettings();
                bool overallAvailability = registryDetection && privacySettings;
                
                Debug.WriteLine($"PhotoCaptureService: Registry detection: {registryDetection}, Privacy settings: {privacySettings}");
                
                _cameraAvailable = overallAvailability;
                return overallAvailability;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error validating camera availability: {ex.Message}");
                _cameraAvailable = false;
                return false;
            }
        }

        public string GetPhotoDirectory() => _photoDirectory;

        public async Task<bool> VerifyPhotoFileAsync(string photoPath)
        {
            try
            {
                return await Task.Run(() =>
                {
                    if (string.IsNullOrWhiteSpace(photoPath))
                        return false;
                    bool fileExists = File.Exists(photoPath);
                    bool isValidPath = photoPath.Contains(_photoDirectory) && Path.GetExtension(photoPath).Equals(".jpg", StringComparison.OrdinalIgnoreCase);
                    return fileExists && isValidPath;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error verifying photo file {photoPath}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeletePhotoAsync(string photoPath)
        {
            try
            {
                return await Task.Run(() =>
                {
                    if (string.IsNullOrWhiteSpace(photoPath) || !photoPath.StartsWith(_photoDirectory, StringComparison.OrdinalIgnoreCase))
                        return false;
                    if (File.Exists(photoPath))
                    {
                        File.Delete(photoPath);
                        return true;
                    }
                    return false;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error deleting photo file {photoPath}: {ex.Message}");
                return false;
            }
        }

        public async Task<int> GetPhotoCountAsync()
        {
            try
            {
                return await Task.Run(() =>
                {
                    if (!Directory.Exists(_photoDirectory))
                        return 0;
                    var jpegFiles = Directory.GetFiles(_photoDirectory, "*.jpg", SearchOption.TopDirectoryOnly);
                    return jpegFiles.Length;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error getting photo count: {ex.Message}");
                return 0;
            }
        }

        // PHASE 5.2: Enhanced camera initialization with preferred device selection
        private async Task<bool> InitializeCameraAsync()
        {
            try
            {
                Debug.WriteLine("PhotoCaptureService: InitializeCameraAsync - Starting camera initialization...");
                await ReleaseCameraAsync();
                
                _mediaCapture = new MediaCapture();
                var mediaCaptureSettings = new MediaCaptureInitializationSettings();
                
                string? selectedCameraId = null;
                
                // PHASE 5.4: Enhanced preferred camera selection with validation
                if (!string.IsNullOrWhiteSpace(_preferredCameraId))
                {
                    Debug.WriteLine($"PhotoCaptureService: Attempting to initialize preferred camera: {_preferredCameraId}");
                    try
                    {
                        var preferredDevice = await DeviceInformation.CreateFromIdAsync(_preferredCameraId);
                        if (preferredDevice != null && preferredDevice.IsEnabled)
                        {
                            selectedCameraId = _preferredCameraId;
                            mediaCaptureSettings.VideoDeviceId = _preferredCameraId;
                            Debug.WriteLine($"PhotoCaptureService: Using preferred camera: {preferredDevice.Name}");
                        }
                        else
                        {
                            Debug.WriteLine("PhotoCaptureService: Preferred camera not available or disabled, falling back");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"PhotoCaptureService: Error accessing preferred camera: {ex.Message}");
                    }
                }
                
                // Fallback to first available camera
                if (string.IsNullOrWhiteSpace(selectedCameraId))
                {
                    Debug.WriteLine("PhotoCaptureService: Searching for fallback camera...");
                    var videoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                    var availableDevice = videoDevices.FirstOrDefault(d => d.IsEnabled);
                    
                    if (availableDevice != null)
                    {
                        selectedCameraId = availableDevice.Id;
                        mediaCaptureSettings.VideoDeviceId = availableDevice.Id;
                        Debug.WriteLine($"PhotoCaptureService: Using fallback camera: {availableDevice.Name} (ID: {availableDevice.Id})");
                    }
                    else
                    {
                        Debug.WriteLine("PhotoCaptureService: No available camera devices found");
                        return false;
                    }
                }
                
                // PHASE 5.4: Enhanced MediaCapture configuration
                mediaCaptureSettings.StreamingCaptureMode = StreamingCaptureMode.Video;
                mediaCaptureSettings.PhotoCaptureSource = PhotoCaptureSource.VideoPreview;
                mediaCaptureSettings.MemoryPreference = MediaCaptureMemoryPreference.Auto;
                mediaCaptureSettings.SharingMode = MediaCaptureSharingMode.SharedReadOnly;
                
                Debug.WriteLine($"PhotoCaptureService: Initializing MediaCapture with device ID: {selectedCameraId}");
                
                await _mediaCapture.InitializeAsync(mediaCaptureSettings);
                _mediaCaptureInitialized = true;
                _cameraAvailable = true;
                
                Debug.WriteLine("PhotoCaptureService: MediaCapture initialized successfully - READY FOR REAL PHOTO CAPTURE");
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error initializing camera: {ex.Message}");
                Debug.WriteLine($"PhotoCaptureService: Exception type: {ex.GetType().Name}");
                Debug.WriteLine($"PhotoCaptureService: Stack trace: {ex.StackTrace}");
                
                _mediaCaptureInitialized = false;
                _cameraAvailable = false;
                
                // Clean up on failure
                if (_mediaCapture != null)
                {
                    try
                    {
                        _mediaCapture.Dispose();
                        _mediaCapture = null;
                    }
                    catch (Exception disposeEx)
                    {
                        Debug.WriteLine($"PhotoCaptureService: Error disposing MediaCapture after init failure: {disposeEx.Message}");
                    }
                }
                
                return false;
            }
        }

        private async Task<byte[]?> CaptureImageBytesAsync()
        {
            try
            {
                Debug.WriteLine("PhotoCaptureService: CaptureImageBytesAsync - Starting real camera capture...");
                
                if (_mediaCapture == null || !_mediaCaptureInitialized)
                {
                    Debug.WriteLine("PhotoCaptureService: MediaCapture is null or not initialized");
                    return null;
                }
                
                Debug.WriteLine("PhotoCaptureService: MediaCapture is ready, preparing photo capture...");
                
                // PHASE 5.4: Enhanced real photo capture with better error handling
                var lowLagCapture = await _mediaCapture.PrepareLowLagPhotoCaptureAsync(ImageEncodingProperties.CreateJpeg());
                Debug.WriteLine("PhotoCaptureService: LowLagPhotoCapture prepared successfully");
                
                var capturedPhoto = await lowLagCapture.CaptureAsync();
                Debug.WriteLine("PhotoCaptureService: Photo capture attempt completed");
                
                if (capturedPhoto?.Frame != null)
                {
                    Debug.WriteLine($"PhotoCaptureService: Photo frame captured, size: {capturedPhoto.Frame.Size} bytes");
                    
                    using (var inputStream = capturedPhoto.Frame)
                    using (var dataReader = new DataReader(inputStream.GetInputStreamAt(0)))
                    {
                        await dataReader.LoadAsync((uint)inputStream.Size);
                        var photoBytes = new byte[inputStream.Size];
                        dataReader.ReadBytes(photoBytes);
                        
                        Debug.WriteLine($"PhotoCaptureService: Successfully read {photoBytes.Length} bytes from camera");
                        
                        await lowLagCapture.FinishAsync();
                        return photoBytes;
                    }
                }
                else
                {
                    Debug.WriteLine("PhotoCaptureService: Captured photo frame is null");
                    await lowLagCapture.FinishAsync();
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error in CaptureImageBytesAsync: {ex.Message}");
                Debug.WriteLine($"PhotoCaptureService: Exception type: {ex.GetType().Name}");
                Debug.WriteLine($"PhotoCaptureService: Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        private async Task ReleaseCameraAsync()
        {
            try
            {
                if (_mediaCapture != null)
                {
                    _mediaCapture.Dispose();
                    _mediaCapture = null;
                }
                _mediaCaptureInitialized = false;
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error releasing camera: {ex.Message}");
            }
        }

        private async Task InitializeCameraSettingsAsync()
        {
            try
            {
                var settings = await _cameraSettingsService.LoadSettingsAsync();
                _preferredCameraId = settings.SelectedCameraId;
                if (!string.IsNullOrWhiteSpace(_preferredCameraId))
                {
                    Debug.WriteLine($"PhotoCaptureService: Loaded preferred camera ID: {_preferredCameraId}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error initializing camera settings: {ex.Message}");
                _preferredCameraId = null;
            }
        }

        private async Task<byte[]> CompressPhotoAsync(byte[] originalPhoto)
        {
            try
            {
                return await Task.Run(() =>
                {
                    using (var originalStream = new MemoryStream(originalPhoto))
                    using (var originalImage = Image.FromStream(originalStream))
                    using (var resizedImage = new Bitmap(320, 240))
                    using (var graphics = Graphics.FromImage(resizedImage))
                    {
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(originalImage, 0, 0, 320, 240);
                        
                        using (var outputStream = new MemoryStream())
                        {
                            var jpegCodec = ImageCodecInfo.GetImageDecoders().FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
                            if (jpegCodec != null)
                            {
                                var encoderParams = new EncoderParameters(1);
                                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 85L);
                                resizedImage.Save(outputStream, jpegCodec, encoderParams);
                            }
                            else
                            {
                                resizedImage.Save(outputStream, ImageFormat.Jpeg);
                            }
                            return outputStream.ToArray();
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error compressing photo: {ex.Message}");
                return originalPhoto;
            }
        }

        private string GeneratePhotoFileName(int employeeId, string photoType)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"{timestamp}_emp{employeeId}_{photoType}.jpg";
        }

        private async Task<string?> SavePhotoAsync(byte[] photoData, string fileName)
        {
            try
            {
                if (!Directory.Exists(_photoDirectory))
                    Directory.CreateDirectory(_photoDirectory);
                
                string fullPath = Path.Combine(_photoDirectory, fileName);
                await File.WriteAllBytesAsync(fullPath, photoData);
                return fullPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error saving photo {fileName}: {ex.Message}");
                return null;
            }
        }

        private async Task<string?> CaptureSimulatedPhotoAsync(int employeeId, string photoType)
        {
            try
            {
                string fileName = GeneratePhotoFileName(employeeId, photoType);
                string fullPath = Path.Combine(_photoDirectory, fileName);
                byte[] simulatedPhoto = await CreateSimulatedPhotoAsync(employeeId, photoType);
                await File.WriteAllBytesAsync(fullPath, simulatedPhoto);
                return fullPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error creating simulated photo: {ex.Message}");
                return null;
            }
        }

        private async Task<byte[]> CreateSimulatedPhotoAsync(int employeeId, string photoType)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var bitmap = new Bitmap(320, 240))
                    using (var graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.Clear(Color.FromArgb(240, 248, 255));
                        using (var borderPen = new Pen(Color.FromArgb(70, 130, 180), 2))
                        {
                            graphics.DrawRectangle(borderPen, 1, 1, 318, 238);
                        }
                        
                        using (var font = new Font("Arial", 10, FontStyle.Bold))
                        using (var brush = new SolidBrush(Color.FromArgb(25, 25, 112)))
                        {
                            // PHASE 5.4: Enhanced simulation photo with clearer status indication
                            var cameraStatus = _cameraAvailable ? "Real Camera Available" : "No Camera Hardware";
                            var operationMode = "SIMULATION MODE ACTIVE";
                            var preferredCamera = !string.IsNullOrWhiteSpace(_preferredCameraId) ? "Camera Selected But Not Used" : "No Camera Preference";
                            var reason = _cameraAvailable ? "Hardware Error or Permission Issue" : "No Physical Camera Detected";
                            
                            var text = $"Employee ID: {employeeId}\n{photoType.ToUpper()} Photo\n{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n{operationMode}\n{cameraStatus}\n{preferredCamera}\n{reason}";
                            var rect = new RectangleF(10, 10, 300, 220);
                            var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                            graphics.DrawString(text, font, brush, rect, format);
                        }
                        
                        using (var stream = new MemoryStream())
                        {
                            var jpegCodec = ImageCodecInfo.GetImageDecoders().FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
                            if (jpegCodec != null)
                            {
                                var encoderParams = new EncoderParameters(1);
                                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 85L);
                                bitmap.Save(stream, jpegCodec, encoderParams);
                            }
                            else
                            {
                                bitmap.Save(stream, ImageFormat.Jpeg);
                            }
                            return stream.ToArray();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"PhotoCaptureService: Error creating simulated photo bitmap: {ex.Message}");
                    return new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
                }
            });
        }

        private bool CheckCameraDevicesInRegistry()
        {
            try
            {
                string[] cameraRegistryPaths = {
                    @"SYSTEM\CurrentControlSet\Control\DeviceClasses\{65e8773d-8f56-11d0-a3b9-00a0c9223196}",
                    @"SYSTEM\CurrentControlSet\Control\DeviceClasses\{6bdd1fc6-810f-11d0-bec7-08002be2092f}",
                    @"SYSTEM\CurrentControlSet\Enum\USB"
                };

                foreach (string registryPath in cameraRegistryPaths)
                {
                    try
                    {
                        using (var key = Registry.LocalMachine.OpenSubKey(registryPath))
                        {
                            if (key != null)
                            {
                                var subKeyNames = key.GetSubKeyNames();
                                foreach (string subKeyName in subKeyNames.Take(10))
                                {
                                    if (subKeyName.ToLowerInvariant().Contains("camera") ||
                                        subKeyName.ToLowerInvariant().Contains("webcam") ||
                                        subKeyName.ToLowerInvariant().Contains("video") ||
                                        subKeyName.ToLowerInvariant().Contains("imaging"))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"PhotoCaptureService: Error checking registry path {registryPath}: {ex.Message}");
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error in registry camera detection: {ex.Message}");
                return false;
            }
        }

        private bool CheckCameraPrivacySettings()
        {
            try
            {
                string privacyRegPath = @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam";
                using (var key = Registry.CurrentUser.OpenSubKey(privacyRegPath))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("Value");
                        if (value != null)
                        {
                            string privacySetting = value.ToString();
                            return privacySetting.Equals("Allow", StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
                return true; // Default to allow if can't determine
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error in privacy settings check: {ex.Message}");
                return true;
            }
        }

        private bool TestCameraAccess()
        {
            try
            {
                // Placeholder for actual camera access testing
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error in camera access test: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            try
            {
                if (_mediaCapture != null)
                {
                    _mediaCapture.Dispose();
                    _mediaCapture = null;
                }
                _mediaCaptureInitialized = false;
                
                // PHASE 5.3: Stop camera monitoring on disposal
                StopCameraMonitoring();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error during disposal: {ex.Message}");
            }
        }

        #region Phase 5.3: Dynamic Camera Monitoring

        /// <summary>
        /// Starts monitoring camera device changes using DeviceWatcher.
        /// PHASE 5.3: Dynamic camera monitoring implementation.
        /// </summary>
        public void StartCameraMonitoring()
        {
            try
            {
                if (_isMonitoring || _deviceWatcher != null)
                {
                    Debug.WriteLine("PhotoCaptureService: Camera monitoring already active");
                    return;
                }

                Debug.WriteLine("PhotoCaptureService: Starting dynamic camera monitoring...");

                // Create DeviceWatcher for video capture devices
                _deviceWatcher = DeviceInformation.CreateWatcher(DeviceClass.VideoCapture);

                // Subscribe to device events
                _deviceWatcher.Added += OnCameraDeviceAdded;
                _deviceWatcher.Removed += OnCameraDeviceRemoved;
                _deviceWatcher.Updated += OnCameraDeviceUpdated;
                _deviceWatcher.Stopped += OnCameraWatcherStopped;
                _deviceWatcher.EnumerationCompleted += OnCameraEnumerationCompleted;

                // Start monitoring
                _deviceWatcher.Start();
                _isMonitoring = true;

                Debug.WriteLine("PhotoCaptureService: Dynamic camera monitoring started successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error starting camera monitoring: {ex.Message}");
                _isMonitoring = false;
            }
        }

        /// <summary>
        /// Stops monitoring camera device changes.
        /// PHASE 5.3: Dynamic camera monitoring implementation.
        /// </summary>
        public void StopCameraMonitoring()
        {
            try
            {
                if (!_isMonitoring || _deviceWatcher == null)
                {
                    return;
                }

                Debug.WriteLine("PhotoCaptureService: Stopping dynamic camera monitoring...");

                // Unsubscribe from events
                _deviceWatcher.Added -= OnCameraDeviceAdded;
                _deviceWatcher.Removed -= OnCameraDeviceRemoved;
                _deviceWatcher.Updated -= OnCameraDeviceUpdated;
                _deviceWatcher.Stopped -= OnCameraWatcherStopped;
                _deviceWatcher.EnumerationCompleted -= OnCameraEnumerationCompleted;

                // Stop the watcher
                if (_deviceWatcher.Status == DeviceWatcherStatus.Started ||
                    _deviceWatcher.Status == DeviceWatcherStatus.EnumerationCompleted)
                {
                    _deviceWatcher.Stop();
                }

                _deviceWatcher = null;
                _isMonitoring = false;

                Debug.WriteLine("PhotoCaptureService: Dynamic camera monitoring stopped");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error stopping camera monitoring: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the current monitoring status.
        /// PHASE 5.3: Dynamic camera monitoring implementation.
        /// </summary>
        public bool IsMonitoring => _isMonitoring;

        /// <summary>
        /// Event handler for when a camera device is added to the system.
        /// PHASE 5.3: Dynamic camera monitoring implementation.
        /// </summary>
        private void OnCameraDeviceAdded(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            try
            {
                Debug.WriteLine($"PhotoCaptureService: Camera device added - {deviceInfo.Name} (ID: {deviceInfo.Id})");

                // Check if this is the previously selected camera that was disconnected
                bool isPreferredCamera = !string.IsNullOrWhiteSpace(_preferredCameraId) && 
                                       string.Equals(_preferredCameraId, deviceInfo.Id, StringComparison.OrdinalIgnoreCase);

                if (isPreferredCamera)
                {
                    Debug.WriteLine($"PhotoCaptureService: Preferred camera reconnected - {deviceInfo.Name}");
                }

                // Notify listeners about the device addition
                CameraDeviceChanged?.Invoke(this, new CameraDeviceChangedEventArgs(
                    deviceInfo.Id, 
                    deviceInfo.Name, 
                    CameraChangeType.Added, 
                    isPreferredCamera));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error handling camera device added: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler for when a camera device is removed from the system.
        /// PHASE 5.3: Dynamic camera monitoring implementation.
        /// </summary>
        private void OnCameraDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate deviceUpdate)
        {
            try
            {
                Debug.WriteLine($"PhotoCaptureService: Camera device removed - ID: {deviceUpdate.Id}");

                // Check if this is the currently preferred/active camera
                bool isPreferredCamera = !string.IsNullOrWhiteSpace(_preferredCameraId) && 
                                       string.Equals(_preferredCameraId, deviceUpdate.Id, StringComparison.OrdinalIgnoreCase);

                if (isPreferredCamera)
                {
                    Debug.WriteLine($"PhotoCaptureService: ALERT - Preferred camera disconnected! ID: {deviceUpdate.Id}");
                    
                    // Release the current MediaCapture since the device is gone
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ReleaseCameraAsync();
                            _cameraAvailable = false;
                            Debug.WriteLine("PhotoCaptureService: Released MediaCapture for disconnected preferred camera");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"PhotoCaptureService: Error releasing camera on disconnect: {ex.Message}");
                        }
                    });
                }

                // Notify listeners about the device removal
                CameraDeviceChanged?.Invoke(this, new CameraDeviceChangedEventArgs(
                    deviceUpdate.Id, 
                    "Unknown Camera", // Name not available in update
                    CameraChangeType.Removed, 
                    isPreferredCamera));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error handling camera device removed: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler for when a camera device is updated.
        /// PHASE 5.3: Dynamic camera monitoring implementation.
        /// </summary>
        private void OnCameraDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate deviceUpdate)
        {
            try
            {
                Debug.WriteLine($"PhotoCaptureService: Camera device updated - ID: {deviceUpdate.Id}");

                // Check if this affects the preferred camera
                bool isPreferredCamera = !string.IsNullOrWhiteSpace(_preferredCameraId) && 
                                       string.Equals(_preferredCameraId, deviceUpdate.Id, StringComparison.OrdinalIgnoreCase);

                // Notify listeners about the device update
                CameraDeviceChanged?.Invoke(this, new CameraDeviceChangedEventArgs(
                    deviceUpdate.Id, 
                    "Updated Camera", // Name not available in update
                    CameraChangeType.Updated, 
                    isPreferredCamera));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoCaptureService: Error handling camera device updated: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler for when the DeviceWatcher stops.
        /// PHASE 5.3: Dynamic camera monitoring implementation.
        /// </summary>
        private void OnCameraWatcherStopped(DeviceWatcher sender, object args)
        {
            Debug.WriteLine("PhotoCaptureService: Camera device watcher stopped");
            _isMonitoring = false;
        }

        /// <summary>
        /// Event handler for when the initial device enumeration is completed.
        /// PHASE 5.3: Dynamic camera monitoring implementation.
        /// </summary>
        private void OnCameraEnumerationCompleted(DeviceWatcher sender, object args)
        {
            Debug.WriteLine("PhotoCaptureService: Initial camera device enumeration completed");
        }

        #endregion
    }
}
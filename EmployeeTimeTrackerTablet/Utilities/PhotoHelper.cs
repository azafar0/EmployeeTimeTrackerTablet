using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace EmployeeTimeTrackerTablet.Utilities
{
    /// <summary>
    /// Provides comprehensive static utility methods for managing photo files in the Employee Time Tracker application.
    /// Handles directory creation, path generation, image compression, file operations, and cleanup for photo management.
    /// </summary>
    public static class PhotoHelper
    {
        private static string? _cachedPhotoDirectory;
        private static readonly object _directoryLock = new object();

        /// <summary>
        /// Ensures the dedicated "photos/" subdirectory exists within the application's data folder.
        /// Uses cached directory path for performance optimization.
        /// </summary>
        /// <returns>The full path to the created/existing photo directory, or null on failure.</returns>
        public static async Task<string?> CreatePhotoDirectory()
        {
            try
            {
                return await Task.Run(() =>
                {
                    lock (_directoryLock)
                    {
                        if (!string.IsNullOrEmpty(_cachedPhotoDirectory) && Directory.Exists(_cachedPhotoDirectory))
                        {
                            return _cachedPhotoDirectory;
                        }

                        // Use application base directory with photos subdirectory
                        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                        string photoDirectory = Path.Combine(baseDirectory, "photos");

                        try
                        {
                            if (!Directory.Exists(photoDirectory))
                            {
                                Directory.CreateDirectory(photoDirectory);
                                Debug.WriteLine($"PhotoHelper: Created photo directory at {photoDirectory}");
                            }

                            _cachedPhotoDirectory = photoDirectory;
                            Debug.WriteLine($"PhotoHelper: Photo directory ready at {photoDirectory}");
                            return photoDirectory;
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            Debug.WriteLine($"PhotoHelper: Access denied creating photo directory: {ex.Message}");
                            return null;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"PhotoHelper: Error creating photo directory: {ex.Message}");
                            return null;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoHelper: Unexpected error in CreatePhotoDirectory: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Returns the full absolute path for a photo file given its file name.
        /// Automatically ensures the photo directory exists before returning the path.
        /// </summary>
        /// <param name="fileName">The file name to combine with the photo directory path.</param>
        /// <returns>The full path string, or null if directory cannot be determined.</returns>
        public static string GetPhotoPath(string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    Debug.WriteLine("PhotoHelper: Cannot generate path - fileName is null or empty");
                    return null;
                }

                // Use cached directory if available, otherwise create it synchronously
                string photoDirectory = _cachedPhotoDirectory;
                if (string.IsNullOrEmpty(photoDirectory))
                {
                    // Synchronous creation for immediate path needs
                    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    photoDirectory = Path.Combine(baseDirectory, "photos");
                    
                    try
                    {
                        if (!Directory.Exists(photoDirectory))
                        {
                            Directory.CreateDirectory(photoDirectory);
                        }
                        _cachedPhotoDirectory = photoDirectory;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"PhotoHelper: Error ensuring photo directory exists: {ex.Message}");
                        return null;
                    }
                }

                string fullPath = Path.Combine(photoDirectory, fileName);
                Debug.WriteLine($"PhotoHelper: Generated photo path: {fullPath}");
                return fullPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoHelper: Error generating photo path for {fileName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Optimizes a raw photo byte array and saves it as a JPEG to the specified path.
        /// Resizes to 320x240 pixels and applies JPEG compression with 85% quality.
        /// </summary>
        /// <param name="photoData">Raw photo data to process.</param>
        /// <param name="filePath">Full path where the optimized photo should be saved.</param>
        /// <returns>True on success, false on failure.</returns>
        public static async Task<bool> CompressAndSavePhoto(byte[] photoData, string filePath)
        {
            try
            {
                if (photoData == null || photoData.Length == 0)
                {
                    Debug.WriteLine("PhotoHelper: Cannot compress - photoData is null or empty");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(filePath))
                {
                    Debug.WriteLine("PhotoHelper: Cannot save - filePath is null or empty");
                    return false;
                }

                Debug.WriteLine($"PhotoHelper: Starting compression and save for {photoData.Length} bytes to {filePath}");

                return await Task.Run(() =>
                {
                    try
                    {
                        // Ensure directory exists
                        string directory = Path.GetDirectoryName(filePath);
                        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        using (var originalStream = new MemoryStream(photoData))
                        using (var originalImage = Image.FromStream(originalStream))
                        {
                            // Create resized image (320x240 as specified)
                            using (var resizedImage = new Bitmap(320, 240))
                            using (var graphics = Graphics.FromImage(resizedImage))
                            {
                                // Set high quality rendering
                                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                                // Draw the resized image
                                graphics.DrawImage(originalImage, 0, 0, 320, 240);

                                // Save with JPEG compression (85% quality)
                                var jpegCodec = ImageCodecInfo.GetImageDecoders()
                                    .FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);

                                if (jpegCodec != null)
                                {
                                    var encoderParams = new EncoderParameters(1);
                                    encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 85L);
                                    resizedImage.Save(filePath, jpegCodec, encoderParams);
                                }
                                else
                                {
                                    resizedImage.Save(filePath, ImageFormat.Jpeg);
                                }

                                Debug.WriteLine($"PhotoHelper: Successfully compressed and saved photo to {filePath}");
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"PhotoHelper: Error during compression and save: {ex.Message}");
                        Debug.WriteLine($"PhotoHelper: Stack trace: {ex.StackTrace}");
                        return false;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoHelper: Unexpected error in CompressAndSavePhoto: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Returns the size of an individual photo file in bytes.
        /// Uses FileInfo for reliable file size determination.
        /// </summary>
        /// <param name="filePath">Full path to the photo file.</param>
        /// <returns>File size as long, or -1 if the file doesn't exist or an error occurs.</returns>
        public static long GetPhotoFileSize(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    Debug.WriteLine("PhotoHelper: Cannot get file size - filePath is null or empty");
                    return -1;
                }

                if (!File.Exists(filePath))
                {
                    Debug.WriteLine($"PhotoHelper: File not found for size check: {filePath}");
                    return -1;
                }

                var fileInfo = new FileInfo(filePath);
                long size = fileInfo.Length;
                Debug.WriteLine($"PhotoHelper: File size for {filePath}: {size} bytes");
                return size;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoHelper: Error getting file size for {filePath}: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Deletes photo files older than the specified cutoff date.
        /// Designed for cleanup operations to manage storage space (targets photos older than 4 weeks).
        /// </summary>
        /// <param name="cutoffDate">Delete photos older than this date.</param>
        /// <returns>Task representing the asynchronous cleanup operation.</returns>
        public static async Task CleanupOldPhotos(DateTime cutoffDate)
        {
            try
            {
                Debug.WriteLine($"PhotoHelper: Starting cleanup of photos older than {cutoffDate:yyyy-MM-dd HH:mm:ss}");

                string photoDirectory = await CreatePhotoDirectory();
                if (string.IsNullOrEmpty(photoDirectory))
                {
                    Debug.WriteLine("PhotoHelper: Cannot cleanup - photo directory not available");
                    return;
                }

                await Task.Run(() =>
                {
                    try
                    {
                        var photoFiles = Directory.GetFiles(photoDirectory, "*.jpg", SearchOption.TopDirectoryOnly);
                        int deletedCount = 0;
                        int errorCount = 0;

                        foreach (string filePath in photoFiles)
                        {
                            try
                            {
                                var fileInfo = new FileInfo(filePath);
                                
                                // Check both creation time and last write time, use the more recent one
                                DateTime fileDate = fileInfo.CreationTime > fileInfo.LastWriteTime 
                                    ? fileInfo.CreationTime 
                                    : fileInfo.LastWriteTime;

                                if (fileDate < cutoffDate)
                                {
                                    File.Delete(filePath);
                                    deletedCount++;
                                    Debug.WriteLine($"PhotoHelper: Deleted old photo: {filePath} (date: {fileDate:yyyy-MM-dd})");
                                }
                            }
                            catch (UnauthorizedAccessException)
                            {
                                Debug.WriteLine($"PhotoHelper: Access denied deleting file: {filePath}");
                                errorCount++;
                            }
                            catch (IOException ex)
                            {
                                Debug.WriteLine($"PhotoHelper: File in use, cannot delete: {filePath} - {ex.Message}");
                                errorCount++;
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"PhotoHelper: Error deleting file {filePath}: {ex.Message}");
                                errorCount++;
                            }
                        }

                        Debug.WriteLine($"PhotoHelper: Cleanup completed. Deleted: {deletedCount}, Errors: {errorCount}, Total files checked: {photoFiles.Length}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"PhotoHelper: Error during cleanup operation: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoHelper: Unexpected error in CleanupOldPhotos: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculates the total disk space consumed by all photos in the photos directory.
        /// Iterates through all JPEG files and sums their sizes for storage monitoring.
        /// </summary>
        /// <returns>Total size in bytes as long.</returns>
        public static async Task<long> GetPhotoStorageUsage()
        {
            try
            {
                Debug.WriteLine("PhotoHelper: Calculating total photo storage usage...");

                string photoDirectory = await CreatePhotoDirectory();
                if (string.IsNullOrEmpty(photoDirectory))
                {
                    Debug.WriteLine("PhotoHelper: Cannot calculate storage - photo directory not available");
                    return 0;
                }

                return await Task.Run(() =>
                {
                    try
                    {
                        var photoFiles = Directory.GetFiles(photoDirectory, "*.jpg", SearchOption.TopDirectoryOnly);
                        long totalSize = 0;
                        int fileCount = 0;

                        foreach (string filePath in photoFiles)
                        {
                            long fileSize = GetPhotoFileSize(filePath);
                            if (fileSize > 0)
                            {
                                totalSize += fileSize;
                                fileCount++;
                            }
                        }

                        Debug.WriteLine($"PhotoHelper: Storage usage calculated - {fileCount} files, {totalSize} bytes total ({totalSize / 1024.0 / 1024.0:F2} MB)");
                        return totalSize;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"PhotoHelper: Error calculating storage usage: {ex.Message}");
                        return 0;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoHelper: Unexpected error in GetPhotoStorageUsage: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Validates that a photo path/filename adheres to expected patterns and is safe.
        /// Enforces the YYYYMMDD_HHMMSS_empID_type.jpg naming pattern using regex validation.
        /// </summary>
        /// <param name="filePath">The file path to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool ValidatePhotoPath(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    Debug.WriteLine("PhotoHelper: Path validation failed - filePath is null or empty");
                    return false;
                }

                // Extract filename from path
                string fileName = Path.GetFileName(filePath);
                if (string.IsNullOrEmpty(fileName))
                {
                    Debug.WriteLine("PhotoHelper: Path validation failed - cannot extract filename");
                    return false;
                }

                // Check file extension
                if (!fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"PhotoHelper: Path validation failed - file doesn't end with .jpg: {fileName}");
                    return false;
                }

                // Check for invalid path characters
                char[] invalidChars = Path.GetInvalidFileNameChars();
                if (fileName.Any(c => invalidChars.Contains(c)))
                {
                    Debug.WriteLine($"PhotoHelper: Path validation failed - filename contains invalid characters: {fileName}");
                    return false;
                }

                // Validate naming pattern: YYYYMMDD_HHMMSS_empID_type.jpg
                // Pattern explanation:
                // ^\d{8}_\d{6}_emp\d+_[a-zA-Z]+\.jpg$
                // ^            - Start of string
                // \d{8}        - Exactly 8 digits (YYYYMMDD)
                // _            - Underscore
                // \d{6}        - Exactly 6 digits (HHMMSS)
                // _emp         - Literal "_emp"
                // \d+          - One or more digits (employee ID)
                // _            - Underscore
                // [a-zA-Z]+    - One or more letters (photo type)
                // \.jpg        - Literal ".jpg"
                // $            - End of string
                var pattern = @"^\d{8}_\d{6}_emp\d+_[a-zA-Z]+\.jpg$";
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);

                bool isValidPattern = regex.IsMatch(fileName);
                
                if (isValidPattern)
                {
                    Debug.WriteLine($"PhotoHelper: Path validation successful: {fileName}");
                }
                else
                {
                    Debug.WriteLine($"PhotoHelper: Path validation failed - doesn't match expected pattern: {fileName}");
                    Debug.WriteLine($"PhotoHelper: Expected pattern: YYYYMMDD_HHMMSS_empID_type.jpg");
                }

                return isValidPattern;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoHelper: Error validating photo path {filePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Returns information about the photo directory including file count and basic statistics.
        /// Provides directory metadata for monitoring and management purposes.
        /// </summary>
        /// <returns>A DirectoryInfo object or null on error.</returns>
        public static async Task<DirectoryInfo?> GetPhotoDirectoryInfo()
        {
            try
            {
                Debug.WriteLine("PhotoHelper: Getting photo directory information...");

                string photoDirectory = await CreatePhotoDirectory();
                if (string.IsNullOrEmpty(photoDirectory))
                {
                    Debug.WriteLine("PhotoHelper: Cannot get directory info - photo directory not available");
                    return null;
                }

                return await Task.Run(() =>
                {
                    try
                    {
                        var directoryInfo = new DirectoryInfo(photoDirectory);
                        if (!directoryInfo.Exists)
                        {
                            Debug.WriteLine($"PhotoHelper: Directory does not exist: {photoDirectory}");
                            return null;
                        }

                        // Log directory statistics
                        var jpegFiles = directoryInfo.GetFiles("*.jpg");
                        var totalSize = jpegFiles.Sum(f => f.Length);
                        
                        Debug.WriteLine($"PhotoHelper: Directory info - Path: {directoryInfo.FullName}");
                        Debug.WriteLine($"PhotoHelper: Directory info - JPEG files: {jpegFiles.Length}");
                        Debug.WriteLine($"PhotoHelper: Directory info - Total size: {totalSize} bytes ({totalSize / 1024.0 / 1024.0:F2} MB)");
                        Debug.WriteLine($"PhotoHelper: Directory info - Created: {directoryInfo.CreationTime}");
                        Debug.WriteLine($"PhotoHelper: Directory info - Last accessed: {directoryInfo.LastAccessTime}");

                        return directoryInfo;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"PhotoHelper: Error getting directory info: {ex.Message}");
                        return null;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoHelper: Unexpected error in GetPhotoDirectoryInfo: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the count of JPEG photo files in the photo directory.
        /// Utility method for quick storage monitoring without full directory info.
        /// </summary>
        /// <returns>Number of JPEG files in the photo directory.</returns>
        public static async Task<int> GetPhotoFileCount()
        {
            try
            {
                string photoDirectory = await CreatePhotoDirectory();
                if (string.IsNullOrEmpty(photoDirectory))
                {
                    return 0;
                }

                return await Task.Run(() =>
                {
                    try
                    {
                        var jpegFiles = Directory.GetFiles(photoDirectory, "*.jpg", SearchOption.TopDirectoryOnly);
                        int count = jpegFiles.Length;
                        Debug.WriteLine($"PhotoHelper: Photo file count: {count}");
                        return count;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"PhotoHelper: Error counting photo files: {ex.Message}");
                        return 0;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoHelper: Unexpected error in GetPhotoFileCount: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Validates that photo data represents a valid JPEG image.
        /// Performs basic image format validation before processing.
        /// </summary>
        /// <param name="photoData">Raw photo data to validate.</param>
        /// <returns>True if valid JPEG data, false otherwise.</returns>
        public static bool ValidatePhotoData(byte[] photoData)
        {
            try
            {
                if (photoData == null || photoData.Length < 10)
                {
                    Debug.WriteLine("PhotoHelper: Photo data validation failed - data is null or too small");
                    return false;
                }

                // Check JPEG header (FF D8 FF)
                if (photoData[0] == 0xFF && photoData[1] == 0xD8 && photoData[2] == 0xFF)
                {
                    Debug.WriteLine("PhotoHelper: Photo data validation successful - valid JPEG header detected");
                    return true;
                }

                Debug.WriteLine("PhotoHelper: Photo data validation failed - invalid JPEG header");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoHelper: Error validating photo data: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generates a standardized photo filename following the established naming convention.
        /// Creates names in the format: YYYYMMDD_HHMMSS_empID_type.jpg
        /// </summary>
        /// <param name="employeeId">Employee ID for the photo.</param>
        /// <param name="photoType">Type of photo (e.g., "ClockIn", "ClockOut").</param>
        /// <returns>Generated filename string.</returns>
        public static string GeneratePhotoFileName(int employeeId, string photoType)
        {
            try
            {
                if (employeeId <= 0)
                {
                    Debug.WriteLine("PhotoHelper: Invalid employee ID for filename generation");
                    employeeId = 0; // Use 0 as fallback
                }

                if (string.IsNullOrWhiteSpace(photoType))
                {
                    photoType = "photo"; // Default type
                }

                // Clean photo type (remove invalid characters and spaces)
                photoType = Regex.Replace(photoType, @"[^a-zA-Z0-9]", "");
                if (string.IsNullOrEmpty(photoType))
                {
                    photoType = "photo";
                }

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"{timestamp}_emp{employeeId}_{photoType}.jpg";
                
                Debug.WriteLine($"PhotoHelper: Generated filename: {fileName}");
                return fileName;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PhotoHelper: Error generating filename: {ex.Message}");
                // Return a basic fallback filename
                return $"{DateTime.Now:yyyyMMdd_HHmmss}_emp{employeeId}_photo.jpg";
            }
        }
    }
}
using System;
using System.Collections.Generic;

namespace EmployeeTimeTrackerTablet.Models
{
    /// <summary>
    /// Represents a camera device available in the system.
    /// Used by the admin panel for camera selection and management.
    /// Enhanced with display properties for improved UI presentation.
    /// </summary>
    public class CameraDevice
    {
        /// <summary>
        /// Gets or sets the unique device ID for the camera.
        /// This ID is used by the system to identify and access the specific camera device.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the friendly display name of the camera device.
        /// This name is shown to users in the camera selection interface.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional information about the camera device.
        /// This can include manufacturer, model, or other relevant details.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this camera device is currently available.
        /// A camera might be unavailable if it's in use by another application.
        /// </summary>
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this camera device is currently selected.
        /// Used by the admin panel to track the active camera selection.
        /// </summary>
        public bool IsSelected { get; set; } = false;

        /// <summary>
        /// Gets or sets the manufacturer of the camera device (if available).
        /// Used for enhanced display information in the UI.
        /// </summary>
        public string Manufacturer { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the connection type of the camera (e.g., "USB", "Built-in", "Network").
        /// Used for enhanced display information in the UI.
        /// </summary>
        public string ConnectionType { get; set; } = string.Empty;

        /// <summary>
        /// Gets a user-friendly display name that includes manufacturer if available.
        /// Used for enhanced UI presentation.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(Manufacturer) && !Name.Contains(Manufacturer, StringComparison.OrdinalIgnoreCase))
                {
                    return $"{Manufacturer} {Name}";
                }
                return Name;
            }
        }

        /// <summary>
        /// Gets a short description with key device information.
        /// Used for enhanced UI presentation with concise details.
        /// </summary>
        public string ShortDescription
        {
            get
            {
                var parts = new List<string>();
                
                if (!string.IsNullOrEmpty(ConnectionType))
                {
                    parts.Add(ConnectionType);
                }
                
                if (!string.IsNullOrEmpty(Manufacturer) && !Name.Contains(Manufacturer, StringComparison.OrdinalIgnoreCase))
                {
                    parts.Add($"by {Manufacturer}");
                }
                
                if (parts.Count > 0)
                {
                    return string.Join(" • ", parts);
                }
                
                return !string.IsNullOrEmpty(Description) ? Description : "Video Capture Device";
            }
        }

        /// <summary>
        /// Gets a status display text for the camera device.
        /// Used for enhanced UI presentation with clear status information.
        /// </summary>
        public string StatusText
        {
            get
            {
                return IsAvailable ? "Available" : "In Use or Disabled";
            }
        }

        /// <summary>
        /// Gets an emoji icon representation for the camera device.
        /// Used for enhanced UI presentation with beautiful visual indicators.
        /// </summary>
        public string DeviceIcon
        {
            get
            {
                if (!IsAvailable)
                {
                    return "??"; // Red circle for unavailable camera
                }
                
                return ConnectionType?.ToLowerInvariant() switch
                {
                    "usb" => "??", // Video camera for USB
                    "built-in" => "??", // Laptop for built-in camera  
                    "network" => "??", // Satellite dish for network camera
                    _ => "??" // Photo camera as default
                };
            }
        }

        /// <summary>
        /// Returns the display name for UI binding purposes.
        /// This override ensures that ComboBox and ListBox controls display the camera name correctly.
        /// </summary>
        /// <returns>The friendly name of the camera device.</returns>
        public override string ToString()
        {
            return DisplayName;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current CameraDevice.
        /// Equality is based on the device ID.
        /// </summary>
        /// <param name="obj">The object to compare with the current CameraDevice.</param>
        /// <returns>True if the specified object is equal to the current CameraDevice; otherwise, false.</returns>
        public override bool Equals(object? obj)
        {
            if (obj is CameraDevice other)
            {
                return string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        /// <summary>
        /// Returns a hash code for the current CameraDevice.
        /// The hash code is based on the device ID.
        /// </summary>
        /// <returns>A hash code for the current CameraDevice.</returns>
        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? 0;
        }
    }
}